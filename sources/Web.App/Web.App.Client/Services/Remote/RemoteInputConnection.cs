using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Shared.Models.Remote;
using Web.App.Client.Services.Api;

namespace Web.App.Client.Services.Remote
{
    /// <summary>
    /// Verbindung der Spielseite zum Hub /hubs/remote (per Auth-Cookie). Die Seite meldet sich als Anzeige an,
    /// bekommt Tasten-Ereignisse gekoppelter Handys und veröffentlicht ihren Anzeigezustand.
    /// Pro Browser-Tab ist höchstens eine Spielseite angemeldet.
    /// </summary>
    public sealed class RemoteInputConnection : IAsyncDisposable
    {
        private readonly NavigationManager _navigation;
        private readonly HttpClient _bff;

        private HubConnection? _connection;
        private Func<RemoteCommand, Task>? _onCommand;
        private Func<Task>? _onStateRequested;
        private string? _lastPublished;

        public RemoteInputConnection(NavigationManager navigation, IHttpClientFactory httpClientFactory)
        {
            _navigation = navigation;
            _bff = httpClientFactory.CreateClient(BffHttpClient.Name);
        }

        /// <summary>Namen der verbundenen Handys.</summary>
        public IReadOnlyList<string> Controllers { get; private set; } = [];

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        /// <summary>Verbundene Handys oder Verbindungsstatus haben sich geändert.</summary>
        public event Action? Changed;

        public async Task AttachAsync(Func<RemoteCommand, Task> onCommand, Func<Task> onStateRequested)
        {
            _onCommand = onCommand;
            _onStateRequested = onStateRequested;
            _lastPublished = null;

            try
            {
                if (_connection is null)
                {
                    _connection = new HubConnectionBuilder()
                        .WithUrl(_navigation.ToAbsoluteUri(RemoteHubMethods.HubPath))
                        .WithAutomaticReconnect()
                        .Build();

                    _connection.On<RemoteCommand>(RemoteHubMethods.Command, command => _onCommand?.Invoke(command) ?? Task.CompletedTask);
                    _connection.On(RemoteHubMethods.StateRequested, () =>
                    {
                        _lastPublished = null;
                        return _onStateRequested?.Invoke() ?? Task.CompletedTask;
                    });
                    _connection.On<List<string>>(RemoteHubMethods.ControllersChanged, names =>
                    {
                        Controllers = names;
                        Changed?.Invoke();
                    });

                    // Nach Verbindungsabbruch erneut als Anzeige anmelden und den Stand neu schicken.
                    _connection.Reconnected += async _ =>
                    {
                        if (_onCommand is not null)
                        {
                            _lastPublished = null;
                            await _connection.InvokeAsync(RemoteHubMethods.RegisterDisplay);
                            await (_onStateRequested?.Invoke() ?? Task.CompletedTask);
                        }
                        Changed?.Invoke();
                    };
                    _connection.Closed += _ =>
                    {
                        Controllers = [];
                        Changed?.Invoke();
                        return Task.CompletedTask;
                    };
                }

                if (_connection.State == HubConnectionState.Disconnected)
                {
                    await _connection.StartAsync();
                }

                await _connection.InvokeAsync(RemoteHubMethods.RegisterDisplay);
            }
            catch (Exception)
            {
                // Ohne Hub funktioniert die Seite normal weiter, nur ohne Handy.
            }

            Changed?.Invoke();
        }

        public async Task DetachAsync()
        {
            _onCommand = null;
            _onStateRequested = null;
            Controllers = [];

            if (IsConnected)
            {
                try
                {
                    await _connection!.InvokeAsync(RemoteHubMethods.UnregisterDisplay);
                }
                catch (Exception)
                {
                    // Verbindung ohnehin weg.
                }
            }
        }

        /// <summary>Schickt den Zustand an die Handys, nur wenn er sich geändert hat.</summary>
        public async Task PublishAsync(RemoteDisplayState state)
        {
            if (!IsConnected || _onCommand is null)
            {
                return;
            }

            var json = JsonSerializer.Serialize(state);

            if (json == _lastPublished)
            {
                return;
            }

            _lastPublished = json;

            try
            {
                await _connection!.InvokeAsync(RemoteHubMethods.PublishState, state);
            }
            catch (Exception)
            {
                _lastPublished = null;
            }
        }

        /// <summary>Neuer Kopplungscode mit QR-Code (gilt kurz und nur einmal).</summary>
        public Task<ApiResult<RemotePairingInfo>> CreatePairingAsync(CancellationToken cancellationToken = default) =>
            ApiResponse.ReadAsync<RemotePairingInfo>(_bff.PostAsync("bff/remote/pairing", null, cancellationToken), cancellationToken);

        public async ValueTask DisposeAsync()
        {
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }
        }
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Shared.Enums.Remote;
using Shared.Models.Remote;
using Web.Shared.DI;

namespace Web.App.Remote
{
    /// <summary>
    /// Vermittelt zwischen der geöffneten Spielseite (Browser, Cookie) und gekoppelten Handys (Geräte-Token).
    /// Enthält keine Spiellogik: Tasten-Ereignisse gehen an die aktive Spielseite, der Anzeigezustand an die Handys.
    /// Verbindungen eines Benutzers sehen nur dessen eigene Gruppe.
    /// </summary>
    [Authorize(AuthenticationSchemes = WebServiceRegistration.Scheme + "," + RemoteDeviceAuthenticationHandler.SchemeName)]
    public sealed class RemoteInputHub : Hub
    {
        private readonly RemoteConnectionRegistry _registry;

        public RemoteInputHub(RemoteConnectionRegistry registry)
        {
            _registry = registry;
        }

        private string UserId => Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new HubException("Not authenticated.");

        private bool IsDevice => Context.User?.HasClaim(c => c.Type == RemoteClaimTypes.DeviceId) == true;

        private static string ControllerGroup(string userId) => $"remote:{userId}:controllers";

        // ---------- Spielseite (Browser) ----------

        public async Task RegisterDisplay()
        {
            RequireDisplay();

            var previous = _registry.SetDisplay(UserId, Context.ConnectionId);

            // Ältere Spielseite (zweiter Tab) bekommt keine Eingaben mehr.
            if (previous is not null)
            {
                await Clients.Client(previous).SendAsync(RemoteHubMethods.ControllersChanged, new List<string>());
            }

            await Clients.Caller.SendAsync(RemoteHubMethods.ControllersChanged, _registry.GetControllerNames(UserId));
        }

        public async Task UnregisterDisplay()
        {
            RequireDisplay();

            if (_registry.RemoveDisplay(UserId, Context.ConnectionId))
            {
                await Clients.Group(ControllerGroup(UserId)).SendAsync(RemoteHubMethods.DisplayGone);
            }
        }

        public async Task PublishState(RemoteDisplayState state)
        {
            RequireDisplay();

            if (_registry.IsDisplay(UserId, Context.ConnectionId))
            {
                await Clients.Group(ControllerGroup(UserId)).SendAsync(RemoteHubMethods.State, state);
            }
        }

        // ---------- Handy ----------

        public async Task RegisterController()
        {
            RequireDevice();

            var name = Context.User?.FindFirstValue(RemoteClaimTypes.DeviceName) ?? "Handy";
            _registry.AddController(UserId, Context.ConnectionId, name);
            await Groups.AddToGroupAsync(Context.ConnectionId, ControllerGroup(UserId));

            if (_registry.GetDisplay(UserId) is { } display)
            {
                await Clients.Client(display).SendAsync(RemoteHubMethods.ControllersChanged, _registry.GetControllerNames(UserId));
                await Clients.Client(display).SendAsync(RemoteHubMethods.StateRequested);
            }
            else
            {
                await Clients.Caller.SendAsync(RemoteHubMethods.DisplayGone);
            }
        }

        public async Task SendCommand(RemoteCommand command)
        {
            RequireDevice();

            if (!IsValid(command))
            {
                throw new HubException("Invalid command.");
            }

            if (_registry.GetDisplay(UserId) is { } display)
            {
                await Clients.Client(display).SendAsync(RemoteHubMethods.Command, command);
            }
            else
            {
                await Clients.Caller.SendAsync(RemoteHubMethods.DisplayGone);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId is not null)
            {
                if (IsDevice)
                {
                    if (_registry.RemoveController(userId, Context.ConnectionId) && _registry.GetDisplay(userId) is { } display)
                    {
                        await Clients.Client(display).SendAsync(RemoteHubMethods.ControllersChanged, _registry.GetControllerNames(userId));
                    }
                }
                else if (_registry.RemoveDisplay(userId, Context.ConnectionId))
                {
                    await Clients.Group(ControllerGroup(userId)).SendAsync(RemoteHubMethods.DisplayGone);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        private void RequireDevice()
        {
            if (!IsDevice)
            {
                throw new HubException("Only paired devices can send input.");
            }
        }

        private void RequireDisplay()
        {
            if (IsDevice)
            {
                throw new HubException("Devices cannot act as display.");
            }

            // Cookie-Verbindung nur von der eigenen Seite (Schutz gegen Cross-Site-WebSocket).
            var request = Context.GetHttpContext()?.Request;
            var origin = request?.Headers.Origin.ToString();

            if (request is not null && !string.IsNullOrEmpty(origin) &&
                !string.Equals(origin, $"{request.Scheme}://{request.Host}", StringComparison.OrdinalIgnoreCase))
            {
                throw new HubException("Invalid origin.");
            }
        }

        /// <summary>Gleiche Kodierung wie DartRulesBase: Miss 0×0, 1–20 × 1–3, 25 × 1–2.</summary>
        internal static bool IsValid(RemoteCommand command) => command.Type switch
        {
            RemoteCommandTypeEnum.Dart => command.Segment switch
            {
                0 => command.Multiplier == 0,
                >= 1 and <= 20 => command.Multiplier is >= 1 and <= 3,
                25 => command.Multiplier is 1 or 2,
                _ => false
            },
            RemoteCommandTypeEnum.Undo or RemoteCommandTypeEnum.Submit => true,
            _ => false
        };
    }
}

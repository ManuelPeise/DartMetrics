using global::Shared.Models.User;
using Web.App.Client.Services.Api;

namespace Web.App.Client.Services
{
    /// <summary>
    /// Profil des angemeldeten Spielers für Kopfzeile und Profilseite.
    /// Wird einmal geladen; Änderungen (Name, Profilbild) lösen Changed aus, damit die Kopfzeile mitzieht.
    /// </summary>
    public sealed class PlayerProfileState
    {
        private readonly IProfileApi _profileApi;
        private Task<UserProfileModel?>? _loading;

        public PlayerProfileState(IProfileApi profileApi)
        {
            _profileApi = profileApi;
        }

        public UserProfileModel? Profile { get; private set; }

        public event Action? Changed;

        public Task<UserProfileModel?> EnsureLoadedAsync() => _loading ??= LoadAsync();

        public Task<UserProfileModel?> ReloadAsync()
        {
            _loading = LoadAsync();
            return _loading;
        }

        public void Update(Action<UserProfileModel> change)
        {
            if (Profile is null)
            {
                return;
            }

            change(Profile);
            Changed?.Invoke();
        }

        private async Task<UserProfileModel?> LoadAsync()
        {
            var result = await _profileApi.GetProfileAsync();

            if (result.IsSuccess)
            {
                Profile = result.Value;
                Changed?.Invoke();
            }
            else
            {
                // Beim nächsten Aufruf erneut versuchen.
                _loading = null;
            }

            return Profile;
        }
    }
}

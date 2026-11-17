using System.Collections.Concurrent;

namespace Web.App.Remote
{
    /// <summary>
    /// Wer ist pro Benutzer verbunden: genau eine aktive Spielseite (die zuletzt angemeldete) und beliebig viele Handys.
    /// Nur im Speicher – passt zum einzelnen Web-Container; Verbindungen bauen sich nach einem Neustart neu auf.
    /// </summary>
    public sealed class RemoteConnectionRegistry
    {
        private readonly ConcurrentDictionary<string, UserConnections> _users = new();

        public string? SetDisplay(string userId, string connectionId)
        {
            var user = _users.GetOrAdd(userId, _ => new UserConnections());

            lock (user)
            {
                var previous = user.DisplayConnectionId;
                user.DisplayConnectionId = connectionId;
                return previous == connectionId ? null : previous;
            }
        }

        public bool IsDisplay(string userId, string connectionId) =>
            _users.TryGetValue(userId, out var user) && user.DisplayConnectionId == connectionId;

        public string? GetDisplay(string userId) =>
            _users.TryGetValue(userId, out var user) ? user.DisplayConnectionId : null;

        /// <summary>True, wenn die Verbindung die aktive Spielseite war.</summary>
        public bool RemoveDisplay(string userId, string connectionId)
        {
            if (!_users.TryGetValue(userId, out var user))
            {
                return false;
            }

            lock (user)
            {
                if (user.DisplayConnectionId != connectionId)
                {
                    return false;
                }

                user.DisplayConnectionId = null;
                return true;
            }
        }

        public void AddController(string userId, string connectionId, string deviceName) =>
            _users.GetOrAdd(userId, _ => new UserConnections()).Controllers[connectionId] = deviceName;

        public bool RemoveController(string userId, string connectionId) =>
            _users.TryGetValue(userId, out var user) && user.Controllers.TryRemove(connectionId, out _);

        public List<string> GetControllerNames(string userId) =>
            _users.TryGetValue(userId, out var user) ? user.Controllers.Values.OrderBy(n => n).ToList() : [];

        private sealed class UserConnections
        {
            public string? DisplayConnectionId { get; set; }

            public ConcurrentDictionary<string, string> Controllers { get; } = new();
        }
    }
}

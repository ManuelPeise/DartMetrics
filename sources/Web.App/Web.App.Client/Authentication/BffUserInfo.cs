namespace Web.App.Client.Authentication
{
    /// <summary>Answer of GET /bff/user. Contains no tokens - those never leave the server.</summary>
    public sealed class BffUserInfo
    {
        public bool IsAuthenticated { get; init; }
        public string? UserId { get; init; }
        public string? Name { get; init; }
        public string[] Roles { get; init; } = [];
    }
}

namespace Shared.Models.Authentication
{
    public class AuthenticationRequest
    {
        public string EmailAddress { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}

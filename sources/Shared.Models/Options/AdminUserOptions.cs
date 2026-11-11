
namespace Shared.Models.Options
{
    public class AdminUserOptions
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string EmailAddress { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

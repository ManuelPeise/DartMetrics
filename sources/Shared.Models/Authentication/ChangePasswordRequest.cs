using Shared.Models.User;

namespace Shared.Models.Authentication
{
    public class ChangePasswordRequest : UserCredentialsModelBase
    {
        public string CurrentPassword { get; set; } = null!;
    }
}

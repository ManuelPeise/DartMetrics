namespace Shared.Models.User
{
    public class UserRegistrationRequestModel: UserBaseModel
    {
        public UserCredentialsModelBase UserCredentials { get; set; } = null!;
    }
}

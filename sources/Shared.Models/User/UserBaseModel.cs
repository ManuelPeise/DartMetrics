namespace Shared.Models.User
{
    public class UserBaseModel
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string EmailAddress { get; set; } = null!;

        public string? DisplayName { get; set; }
    }
}

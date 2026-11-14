namespace Shared.Models.User
{
    public class UserProfileModel: UserBaseModel
    {
        public bool IsVerified { get; set; }

        /// <summary>Elo aus Spielen gegen Bots (nur lesend, wird beim Speichern ignoriert).</summary>
        public int Rating { get; set; }
    }
}

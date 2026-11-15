namespace Data.DartContext.Entities.User
{
    /// <summary>
    /// Profilbild eines Benutzers (eigene Tabelle, damit Benutzerabfragen das Bild nicht mitladen).
    /// Das Bild wird im Browser auf AvatarRules.MaxPixels verkleinert; der Server prüft Typ und Größe.
    /// Die aktuelle Version steht in UserEntity.AvatarVersion (Teil der Bild-URL, damit Browser-Caches neu laden).
    /// </summary>
    public class UserAvatarEntity : AEntityBase
    {
        public int UserId { get; set; }

        public UserEntity User { get; set; } = null!;

        public string ContentType { get; set; } = null!;

        public byte[] Data { get; set; } = [];

    }
}

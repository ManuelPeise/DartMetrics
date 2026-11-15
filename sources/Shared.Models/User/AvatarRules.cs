namespace Shared.Models.User
{
    /// <summary>Regeln für Profilbilder (Client verkleinert, Server prüft).</summary>
    public static class AvatarRules
    {
        /// <summary>Kantenlänge, auf die der Browser verkleinert.</summary>
        public const int MaxPixels = 256;

        /// <summary>Obergrenze nach dem Verkleinern; ein 256-px-JPEG hat meist 15–40 KB.</summary>
        public const int MaxBytes = 512 * 1024;

        /// <summary>Format, in das der Browser umwandelt.</summary>
        public const string UploadContentType = "image/jpeg";

        /// <summary>Erlaubte Formate für die Dateiauswahl (vor dem Verkleinern).</summary>
        public const string AcceptedFileTypes = "image/jpeg,image/png,image/webp";
    }

    public class UploadAvatarModel
    {
        /// <summary>Bilddaten (JSON: Base64). Der Typ wird serverseitig aus den Bytes bestimmt.</summary>
        public byte[] Data { get; set; } = [];
    }

    public class UploadAvatarResult
    {
        public string Version { get; set; } = null!;
    }

    public class AvatarImage
    {
        public string ContentType { get; set; } = null!;

        public byte[] Data { get; set; } = [];

        public string Version { get; set; } = null!;
    }
}

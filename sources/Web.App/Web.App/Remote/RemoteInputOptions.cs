namespace Web.App.Remote
{
    /// <summary>Handy als Eingabegerät (Abschnitt "RemoteInput"). Kopplung gilt nur für die laufende Anmeldung.</summary>
    public sealed class RemoteInputOptions
    {
        public const string SectionName = "RemoteInput";

        /// <summary>Wie lange ein QR-Code eingelöst werden kann.</summary>
        public int PairingCodeSeconds { get; set; } = 120;

        /// <summary>Gültigkeit des Geräte-Tokens; endet zusätzlich mit der Browser-Sitzung (Abmelden).</summary>
        public int DeviceTokenHours { get; set; } = 12;

        /// <summary>
        /// Öffentliche Adresse für den QR-Code, z. B. https://darts.example.org. Leer: aus der Anfrage
        /// (hinter einem Proxy nur korrekt mit ForwardedHeaders:Enabled).
        /// </summary>
        public string? PublicBaseUrl { get; set; }
    }
}

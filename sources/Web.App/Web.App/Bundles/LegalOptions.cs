namespace Web.App.Bundles
{
    /// <summary>
    /// Angaben für Impressum und Datenschutzerklärung. Kommen aus der Konfiguration (.env -> LEGAL_*),
    /// damit keine persönlichen Daten im (öffentlichen) Repository stehen. Wer das Projekt selbst betreibt,
    /// trägt hier seine eigenen Daten ein.
    /// </summary>
    public sealed class LegalOptions
    {
        public const string SectionName = "Legal";

        /// <summary>Name der verantwortlichen Person (Betreiber).</summary>
        public string Name { get; set; } = "";

        /// <summary>Straße und Hausnummer.</summary>
        public string Street { get; set; } = "";

        /// <summary>PLZ und Ort.</summary>
        public string City { get; set; } = "";

        public string Country { get; set; } = "Deutschland";

        public string Email { get; set; } = "";

        /// <summary>Optional.</summary>
        public string? Phone { get; set; }

        /// <summary>Stand der Datenschutzerklärung, z. B. "November 2026".</summary>
        public string PrivacyVersion { get; set; } = "November 2026";

        /// <summary>Löschfristen wie in der API (DataRetention), für den Text der Datenschutzerklärung.</summary>
        public int UnverifiedAccountDays { get; set; } = 7;

        public int LogMessageDays { get; set; } = 90;

        /// <summary>true = alle Pflichtangaben gesetzt. Sonst zeigen die Seiten einen Hinweis für den Betreiber.</summary>
        public bool IsComplete =>
            !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Street) &&
            !string.IsNullOrWhiteSpace(City) && !string.IsNullOrWhiteSpace(Email);
    }
}

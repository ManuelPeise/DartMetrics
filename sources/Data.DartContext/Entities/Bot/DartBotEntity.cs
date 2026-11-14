namespace Data.DartContext.Entities.Bot
{
    /// <summary>
    /// Stammdaten eines virtuellen Spielers. Bots werden deaktiviert, nicht gelöscht.
    /// XP, Level und Rating ändern sich laufend; die Fähigkeiten liegen versioniert in <see cref="DartBotProfileVersionEntity"/>.
    /// Aktuelle Profilversion = höchste VersionNumber.
    /// </summary>
    public class DartBotEntity : AEntityBase
    {
        public string Name { get; set; } = null!;

        public string Alias { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        /// <summary>Summe der XpGained aus abgeschlossenen Spielen gegen Menschen.</summary>
        public long ExperiencePoints { get; set; }

        public int DevelopmentLevel { get; set; }

        /// <summary>Elo-Wertung; ändert sich nur in abgeschlossenen Spielen gegen Menschen.</summary>
        public int Rating { get; set; } = 1000;

        public ICollection<DartBotProfileVersionEntity> ProfileVersions { get; set; } = [];

        public ICollection<DartBotGamePlayerEntity> GamePlayers { get; set; } = [];
    }
}

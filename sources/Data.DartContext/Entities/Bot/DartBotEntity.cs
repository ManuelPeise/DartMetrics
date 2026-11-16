namespace Data.DartContext.Entities.Bot
{
    /// <summary>
    /// Stammdaten eines virtuellen Spielers. Bots werden deaktiviert, nicht gelöscht.
    /// XP, Level und Rating ändern sich laufend; die Fähigkeiten liegen versioniert in <see cref="DartBotProfileVersionEntity"/>.
    /// Aktuelle Profilversion = höchste VersionNumber.
    /// </summary>
    public class DartBotEntity : AEntityBase, IConcurrencyStamped
    {
        public string Name { get; set; } = null!;

        public string Alias { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        /// <summary>Summe der XpGained aus abgeschlossenen Spielen gegen Menschen.</summary>
        public long ExperiencePoints { get; set; }

        public int DevelopmentLevel { get; set; }

        /// <summary>V2: Durchschnitt, auf den der Bot durch Spielen zuwachsen kann (null = kein Wachstum über das Level hinaus).</summary>
        public float? PotentialAverage { get; set; }

        /// <summary>Elo-Wertung; ändert sich nur in abgeschlossenen Spielen gegen Menschen.</summary>
        public int Rating { get; set; } = 1000;

        /// <summary>Schützt XP, Elo und Level, wenn zwei Spiele gegen denselben Bot gleichzeitig enden.</summary>
        public Guid ConcurrencyStamp { get; set; }

        public ICollection<DartBotProfileVersionEntity> ProfileVersions { get; set; } = [];

        public ICollection<DartBotGamePlayerEntity> GamePlayers { get; set; } = [];
    }
}

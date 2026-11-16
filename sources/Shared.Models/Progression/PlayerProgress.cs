namespace Shared.Models.Progression
{
    /// <summary>Level, Erfolge und Rekorde des angemeldeten Spielers.</summary>
    public class PlayerProgress
    {
        public int Level { get; set; }
        public long ExperiencePoints { get; set; }

        /// <summary>Gesamt-XP, ab denen das aktuelle bzw. nächste Level gilt (für den Fortschrittsbalken).</summary>
        public long LevelStartXp { get; set; }
        public long NextLevelXp { get; set; }

        public List<AchievementItem> Achievements { get; set; } = [];
        public List<RecordItem> Records { get; set; } = [];

        /// <summary>Seit dem letzten Abruf neu freigeschaltet (werden dabei als gesehen markiert).</summary>
        public List<AchievementItem> NewlyUnlocked { get; set; } = [];
    }

    public class AchievementItem
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";

        /// <summary>1 Bronze, 2 Silber, 3 Gold; 0 = einstufig.</summary>
        public int Tier { get; set; }
        public bool IsUnlocked { get; set; }
        public DateTime? UnlockedAt { get; set; }
        public long Progress { get; set; }
        public long Target { get; set; }
    }

    public class RecordItem
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";

        /// <summary>Anzeige-Wert (z. B. "84,3" oder "170"), null = noch kein Wert.</summary>
        public string? Value { get; set; }
        public DateTime? AchievedAt { get; set; }

        /// <summary>Wo der Rekord aufgestellt wurde (z. B. "gegen Robo Taylor", "Turnier Sommerliga").</summary>
        public string? Context { get; set; }
    }
}

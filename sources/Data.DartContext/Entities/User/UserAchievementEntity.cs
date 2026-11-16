namespace Data.DartContext.Entities.User
{
    /// <summary>Freigeschalteter Erfolg eines Spielers. Die Erfolge selbst stehen im Code (AchievementCatalog).</summary>
    public class UserAchievementEntity : AEntityBase
    {
        public int UserId { get; set; }

        public UserEntity User { get; set; } = null!;

        /// <summary>Schlüssel aus dem Katalog, z. B. "first-180" oder "oneeighty-50".</summary>
        public string AchievementKey { get; set; } = null!;

        /// <summary>Zeitpunkt des Spiels bzw. der Session, in der der Erfolg erreicht wurde.</summary>
        public DateTime UnlockedAt { get; set; }

        /// <summary>Wurde dem Spieler schon angezeigt („Erfolg freigeschaltet“).</summary>
        public bool IsSeen { get; set; }
    }
}

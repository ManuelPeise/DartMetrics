using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DartContext.Entities.Training
{
    /// <summary>
    /// Bob's 27: 21 Runden auf D1 … D20 und Bull, Start mit 27 Punkten, 1–8 Spieler am selben Gerät.
    /// Der Stand wird aus den Zügen berechnet (Logic.Shared.Rules.Bobs27Game), nicht gespeichert.
    /// </summary>
    public class Bobs27TrainingEntity : AEntityBase
    {
        /// <summary>Anfänger-Modus: kein Ausscheiden bei 0 oder weniger Punkten.</summary>
        public bool IsBeginnerMode { get; set; }

        public int TrainingSessionId { get; set; }

        [ForeignKey(nameof(TrainingSessionId))]
        public TrainingSessionEntity TrainingSession { get; set; } = null!;

        public ICollection<Bobs27TrainingPlayerEntity> Players { get; set; } = [];

        public ICollection<Bobs27TrainingTurnEntity> Turns { get; set; } = [];
    }
}

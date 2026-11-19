using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DartContext.Entities.Training
{
    /// <summary>Mitspieler einer Bob's-27-Partie. Spieler 1 ist der angemeldete Benutzer, weitere sind Gäste (nur Name).</summary>
    public class Bobs27TrainingPlayerEntity : AEntityBase
    {
        public int PlayerNumber { get; set; }

        public string Name { get; set; } = "";

        public int Bobs27TrainingId { get; set; }

        [ForeignKey(nameof(Bobs27TrainingId))]
        public Bobs27TrainingEntity Bobs27Training { get; set; } = null!;
    }
}

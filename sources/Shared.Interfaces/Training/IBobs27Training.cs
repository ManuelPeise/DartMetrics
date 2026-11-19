using Shared.Models.Training;

namespace Shared.Interfaces.Training
{
    /// <summary>Bob's 27 (Doubles-Training, 1–8 Spieler am selben Gerät). Jede Aktion liefert den vollständigen Stand.</summary>
    public interface IBobs27Training
    {
        /// <summary>Neueste offene Partie des aktuellen Benutzers, sonst null.</summary>
        Task<Bobs27TrainingState?> GetRunningTrainingAsync();

        Task<Bobs27TrainingState> StartTrainingAsync(StartBobs27TrainingModel model);

        Task<Bobs27TrainingState> AddTurnAsync(Bobs27TrainingTurnModel model);

        Task<Bobs27TrainingState> UndoLastTurnAsync(Bobs27TrainingUndoModel model);

        Task FinishTrainingAsync(int trainingId);

        /// <summary>Stand bzw. Ergebnis einer eigenen Partie (auch beendet).</summary>
        Task<Bobs27TrainingState> GetTrainingAsync(int trainingId);
    }
}

namespace Shared.Enums.Tournament
{
    public enum TournamentKindEnum
    {
        /// <summary>Vom GameMaster angelegt und geleitet; zählt für Elo und XP.</summary>
        Official = 1,

        /// <summary>Vom Spieler für sich selbst und Bots angelegt (Training); kein Elo.</summary>
        Private = 2
    }
}

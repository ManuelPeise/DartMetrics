namespace Shared.Enums.Tournament
{
    public enum TournamentMatchStatusEnum
    {
        /// <summary>Paarung oder Termin steht, noch nicht begonnen (im K.-o.-Baum ggf. noch ohne Gegner).</summary>
        Scheduled = 1,
        Running = 2,
        Finished = 3,

        /// <summary>Freilos: ein Teilnehmer rückt ohne Spiel weiter.</summary>
        Bye = 4
    }
}

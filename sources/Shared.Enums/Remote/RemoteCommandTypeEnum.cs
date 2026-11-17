namespace Shared.Enums.Remote
{
    /// <summary>Tasten-Ereignis eines gekoppelten Eingabegeräts (Handy), siehe docs/remote-input-protocol.md.</summary>
    public enum RemoteCommandTypeEnum
    {
        /// <summary>Ein Dart (Segment, Multiplier; Kodierung wie DartThrowInput).</summary>
        Dart = 1,

        /// <summary>Letzten noch nicht gesendeten Dart zurücknehmen.</summary>
        Undo = 2,

        /// <summary>Aufnahme senden.</summary>
        Submit = 3
    }
}

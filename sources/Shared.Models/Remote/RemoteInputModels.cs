using Shared.Enums.Remote;

namespace Shared.Models.Remote
{
    /// <summary>
    /// Namen der SignalR-Methoden des Hubs /hubs/remote. Das Handy ist ein reines Eingabe- und Anzeigegerät:
    /// Es schickt Tasten-Ereignisse, die geöffnete Spielseite im Browser wertet sie aus und schickt den Anzeigezustand zurück.
    /// </summary>
    public static class RemoteHubMethods
    {
        public const string HubPath = "/hubs/remote";

        // Browser (Spielseite) -> Hub
        public const string RegisterDisplay = "RegisterDisplay";
        public const string UnregisterDisplay = "UnregisterDisplay";
        public const string PublishState = "PublishState";

        // Handy -> Hub
        public const string RegisterController = "RegisterController";
        public const string SendCommand = "SendCommand";

        // Hub -> Browser
        public const string Command = "Command";
        public const string StateRequested = "StateRequested";
        public const string ControllersChanged = "ControllersChanged";

        // Hub -> Handy
        public const string State = "State";
        public const string DisplayGone = "DisplayGone";
    }

    /// <summary>Tasten-Ereignis vom Handy.</summary>
    public class RemoteCommand
    {
        public RemoteCommandTypeEnum Type { get; set; }

        /// <summary>Nur bei Dart: 0 (Miss), 1–20, 25.</summary>
        public int Segment { get; set; }

        /// <summary>Nur bei Dart: 0 (Miss), 1–3; 25 nur 1 oder 2.</summary>
        public int Multiplier { get; set; }
    }

    /// <summary>
    /// Fertiger Anzeigezustand für das Handy. Alle Texte sind von der Spielseite aufbereitet;
    /// das Handy rechnet nichts und zeigt nur an.
    /// </summary>
    public class RemoteDisplayState
    {
        /// <summary>z. B. „X01-Training“, „Du gegen Bob“.</summary>
        public string Title { get; set; } = "";

        /// <summary>z. B. „501 · Double-Out · Leg 2 / 5“.</summary>
        public string? Subtitle { get; set; }

        public List<RemotePlayerState> Players { get; set; } = [];

        /// <summary>Darts der laufenden Aufnahme, gewertete und noch nicht gesendete.</summary>
        public List<RemoteDart> Darts { get; set; } = [];

        /// <summary>Summe der angezeigten Darts.</summary>
        public int VisitTotal { get; set; }

        /// <summary>Empfohlener Checkout-Weg, z. B. „T20 · T20 · Bull“.</summary>
        public string? Checkout { get; set; }

        /// <summary>Statuszeile der Seite, z. B. „Bust – die Aufnahme zählt nicht“.</summary>
        public string? Status { get; set; }

        /// <summary>„success“, „danger“ oder leer.</summary>
        public string? StatusTone { get; set; }

        /// <summary>Hinweis der Vorschau neben „Senden“ (Bust, Checkout!).</summary>
        public string? Hint { get; set; }

        public string? HintTone { get; set; }

        /// <summary>Darf gerade eingegeben werden (eigener Wurf, kein Server-Aufruf läuft)?</summary>
        public bool CanInput { get; set; }

        /// <summary>Keine weiteren Darts (Aufnahme voll oder laut Vorschau zu Ende).</summary>
        public bool InputLocked { get; set; }

        public bool CanUndo { get; set; }

        public bool CanSubmit { get; set; }

        /// <summary>Spiel beendet: das Handy zeigt nur noch das Ergebnis.</summary>
        public bool IsFinished { get; set; }

        /// <summary>Zählt bei jeder neuen Ansage hoch; das Handy spricht CallText einmal je neuer Id.</summary>
        public int CallId { get; set; }

        /// <summary>Ansage der zuletzt beendeten Aufnahme(n): Summe, „No score“, „Game shot!“.</summary>
        public string? CallText { get; set; }
    }

    /// <summary>Ein Dart der Anzeige (Kodierung wie DartThrowInput).</summary>
    public class RemoteDart
    {
        public int Segment { get; set; }

        public int Multiplier { get; set; }
    }

    public class RemotePlayerState
    {
        public string Name { get; set; } = "";

        /// <summary>Hauptwert: Rest (X01), Ziel (Around the Clock, Segment) …</summary>
        public string Value { get; set; } = "";

        /// <summary>Zusatz, z. B. „Legs 2 · Ø 54,3“.</summary>
        public string? Detail { get; set; }

        public bool IsActive { get; set; }
    }

    /// <summary>Antwort an den Browser: Kopplungscode für den QR-Code.</summary>
    public class RemotePairingInfo
    {
        public string Code { get; set; } = "";

        /// <summary>Inhalt des QR-Codes: https://&lt;server&gt;/remote?code=…</summary>
        public string QrPayload { get; set; } = "";

        /// <summary>QR-Code als SVG (vom Server erzeugt).</summary>
        public string QrSvg { get; set; } = "";

        public DateTime ExpiresAtUtc { get; set; }
    }

    /// <summary>Handy-Seite /remote -> POST /remote/pair</summary>
    public class RemotePairRequest
    {
        public string Code { get; set; } = "";

        /// <summary>Anzeigename des Geräts, z. B. „Pixel 8“ (optional, max. 60 Zeichen).</summary>
        public string? DeviceName { get; set; }
    }

    /// <summary>Antwort an das Handy: Geräte-Token für den Hub (Bearer bzw. access_token).</summary>
    public class RemotePairResponse
    {
        public string DeviceToken { get; set; } = "";

        public DateTime ExpiresAtUtc { get; set; }

        public string HubPath { get; set; } = RemoteHubMethods.HubPath;

        public string UserName { get; set; } = "";
    }
}

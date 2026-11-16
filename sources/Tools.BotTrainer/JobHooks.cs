namespace Tools.BotTrainer
{
    /// <summary>
    /// Rückkanal der Befehle, wenn sie vom Worker für einen Trainingsauftrag laufen (auf der Kommandozeile null).
    /// </summary>
    public sealed class JobHooks
    {
        /// <summary>Zwischenstand melden (Text, Fortschritt 0–100 innerhalb des Schritts). Liefert true, wenn abgebrochen werden soll.</summary>
        public required Func<string, int, Task<bool>> ReportAsync { get; init; }

        /// <summary>Id des angelegten Datensatzes (generate) bzw. Modells (train).</summary>
        public int? CreatedId { get; set; }

        /// <summary>compare: mindestens eine Schwelle ist auf keiner Stufe signifikant schlechter.</summary>
        public bool? ComparisonPassed { get; set; }

        /// <summary>Meldet und bricht bei Abbruchwunsch mit OperationCanceledException ab.</summary>
        public async Task ReportOrCancelAsync(string text, int percent)
        {
            if (await ReportAsync(text, Math.Clamp(percent, 0, 100)))
            {
                throw new OperationCanceledException("Abbruch angefordert.");
            }
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace Shared.Models.Options
{
    /// <summary>
    /// Automatische Löschfristen (Datensparsamkeit, DSGVO). Ohne Abschnitt "DataRetention" gelten die Standardwerte.
    /// Die Werte gehören so auch in die Datenschutzerklärung.
    /// </summary>
    public class DataRetentionOptions
    {
        /// <summary>Aus: es wird nichts automatisch gelöscht.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Nicht bestätigte Registrierungen werden nach so vielen Tagen gelöscht.</summary>
        [Range(1, 365)]
        public int UnverifiedAccountDays { get; set; } = 7;

        /// <summary>Einträge im Anwendungs-Log (MessageLog) werden nach so vielen Tagen gelöscht.</summary>
        [Range(1, 3650)]
        public int LogMessageDays { get; set; } = 90;

        /// <summary>Wie oft die Bereinigung läuft.</summary>
        [Range(1, 168)]
        public int IntervalHours { get; set; } = 24;
    }
}

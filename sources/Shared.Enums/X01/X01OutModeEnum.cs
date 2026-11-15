namespace Shared.Enums.X01
{
    /// <summary>Womit ein X01-Leg beendet werden muss.</summary>
    public enum X01OutModeEnum
    {
        /// <summary>Abschluss nur mit einem Double bzw. Bull (Standard).</summary>
        Double = 1,

        /// <summary>
        /// Straight-Out: jeder Dart zählt, Abschluss mit jedem Feld; Rest 1 ist erlaubt.
        /// (Wert 2 war früher Master-Out; Bestandsdaten setzt die Migration auf Double-Out.)
        /// </summary>
        Straight = 2
    }
}

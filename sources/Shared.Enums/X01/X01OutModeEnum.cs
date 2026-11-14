namespace Shared.Enums.X01
{
    /// <summary>Womit ein X01-Leg beendet werden muss.</summary>
    public enum X01OutModeEnum
    {
        /// <summary>Abschluss nur mit einem Double bzw. Bull (Standard).</summary>
        Double = 1,

        /// <summary>Abschluss mit einem Double, Bull oder Triple.</summary>
        Master = 2
    }
}

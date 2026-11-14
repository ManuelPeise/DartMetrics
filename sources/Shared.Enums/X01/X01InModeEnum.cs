namespace Shared.Enums.X01
{
    /// <summary>Wie ein X01-Leg eröffnet wird.</summary>
    public enum X01InModeEnum
    {
        /// <summary>Jeder Dart zählt sofort (Standard).</summary>
        Straight = 1,

        /// <summary>Punkte zählen erst ab dem ersten getroffenen Double (inklusive).</summary>
        Double = 2
    }
}

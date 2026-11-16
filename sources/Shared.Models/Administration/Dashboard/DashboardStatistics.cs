namespace Shared.Models.Administration.Dashboard
{
    /// <summary>Kennzahlen für das Admin-Dashboard (nur Zählwerte, keine personenbezogenen Daten).</summary>
    public class DashboardStatistics
    {
        public int TrainingSessionCount { get; set; }

        public int TrainingSessionsLast7Days { get; set; }
    }
}

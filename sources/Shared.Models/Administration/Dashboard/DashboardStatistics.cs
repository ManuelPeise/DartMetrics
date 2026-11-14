namespace Shared.Models.Administration.Dashboard
{
    /// <summary>Kennzahlen für das Admin-Dashboard (nur Zählwerte, keine personenbezogenen Daten).</summary>
    public class DashboardStatistics
    {
        public int ModelCount { get; set; }

        /// <summary>Modelltypen mit aktivem Modell.</summary>
        public int ActiveModelCount { get; set; }

        public int TrainingSessionCount { get; set; }

        public int TrainingSessionsLast7Days { get; set; }
    }
}

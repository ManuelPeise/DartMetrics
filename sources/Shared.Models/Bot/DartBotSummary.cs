namespace Shared.Models.Bot
{
    /// <summary>Öffentliche Sicht auf einen Bot für die Gegnerwahl (ohne interne Profilwerte).</summary>
    public class DartBotSummary
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string Alias { get; set; } = null!;

        public int DevelopmentLevel { get; set; }

        public int Rating { get; set; }
    }
}

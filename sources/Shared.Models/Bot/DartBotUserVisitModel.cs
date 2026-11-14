namespace Shared.Models.Bot
{
    /// <summary>
    /// Aufnahme des Menschen: jeder Dart einzeln (Miss = 0 x 0).
    /// Genau drei Darts, außer die Aufnahme endet vorher durch Bust oder Checkout.
    /// </summary>
    public class DartBotUserVisitModel
    {
        public int GameId { get; set; }

        public List<DartBotUserThrowModel> Throws { get; set; } = [];
    }

    public class DartBotUserThrowModel
    {
        public int Segment { get; set; }

        public int Multiplier { get; set; }
    }
}

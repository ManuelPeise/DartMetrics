using Shared.Models.Remote;
using Shared.Models.Shared;

namespace Web.App.Client.Components.Player
{
    /// <summary>Hilfen zum Aufbereiten des Anzeigezustands für das Handy (reine Formatierung).</summary>
    public static class RemoteState
    {
        public static List<RemoteDart> Darts(IEnumerable<DartThrowInput> darts) =>
            darts.Select(d => new RemoteDart { Segment = d.Segment, Multiplier = d.Multiplier }).ToList();

        public static int Total(IEnumerable<DartThrowInput> darts) => darts.Sum(d => d.Score);

        /// <summary>Erster (empfohlener) Weg wie im CheckoutHint, z. B. „T20 · T20 · Bull“.</summary>
        public static string? Checkout(IReadOnlyList<CheckoutSuggestion>? suggestions) =>
            suggestions is { Count: > 0 }
                ? string.Join(" · ", suggestions[0].Darts.Select(d => new DartThrowInput(d.Segment, d.Multiplier).Label))
                : null;
    }
}

using Shared.Models.Remote;
using Shared.Models.Shared;
using Shared.Models.User;

namespace Web.App.Client.Components.Player
{
    /// <summary>Hilfen zum Aufbereiten des Anzeigezustands für das Handy (reine Formatierung).</summary>
    public static class RemoteState
    {
        public static List<RemoteDart> Darts(IEnumerable<DartThrowInput> darts) =>
            darts.Select(d => new RemoteDart { Segment = d.Segment, Multiplier = d.Multiplier }).ToList();

        public static int Total(IEnumerable<DartThrowInput> darts) => darts.Sum(d => d.Score);

        /// <summary>Ansage zu Beginn jedes Legs mit dem Spieler, der anwirft: „Game on! Max, throw fast!“</summary>
        public static string LegStartCall(string? starter) =>
            string.IsNullOrWhiteSpace(starter) ? "Game on! Throw fast!" : $"Game on! {starter}, throw fast!";

        /// <summary>Spielende mit Checkout (Bot-Spiel, Turnier).</summary>
        public static string MatchShotCall(string? winner) =>
            string.IsNullOrWhiteSpace(winner) ? "Game shot and the match!" : $"Game shot and the match! {winner} wins!";

        /// <summary>Ende des X01-Trainings (allein, ohne Sieger).</summary>
        public static string TrainingCompleteCall(string? player) =>
            string.IsNullOrWhiteSpace(player) ? "Game shot! Training complete. Well done!" : $"Game shot! Training complete. Well done, {player}!";

        /// <summary>Anzeigename aus dem Profil, sonst der Vorname.</summary>
        public static string? PlayerName(UserProfileModel? profile) =>
            profile is null ? null : string.IsNullOrWhiteSpace(profile.DisplayName) ? profile.FirstName : profile.DisplayName;

        /// <summary>Ansage einer Aufnahme: „Game shot!“ (Leg beendet), „No score“ (auch Bust), „One hundred and eighty!“ oder die Summe.</summary>
        public static string CallText(int scored, bool isBust, bool isCheckout) =>
            isCheckout ? "Game shot!"
            : isBust || scored == 0 ? "No score"
            : scored == 180 ? "One hundred and eighty!"
            : scored.ToString();

        /// <summary>Erster (empfohlener) Weg wie im CheckoutHint, z. B. „T20 · T20 · Bull“.</summary>
        public static string? Checkout(IReadOnlyList<CheckoutSuggestion>? suggestions) =>
            suggestions is { Count: > 0 }
                ? string.Join(" · ", suggestions[0].Darts.Select(d => new DartThrowInput(d.Segment, d.Multiplier).Label))
                : null;
    }
}

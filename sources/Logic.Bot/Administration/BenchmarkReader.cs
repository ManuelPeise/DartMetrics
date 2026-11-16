using System.Text.Json.Nodes;

namespace Logic.Bot.Administration
{
    /// <summary>Liest den Prüfstand-Wert aus MetricsJson eines Trainingslaufs (geschrieben von Tools.BotTrainer compare --benchmark).</summary>
    public static class BenchmarkReader
    {
        public static double? Score(string? metricsJson)
        {
            if (string.IsNullOrWhiteSpace(metricsJson))
            {
                return null;
            }

            try
            {
                var comparison = JsonNode.Parse(metricsJson)?["Comparison"];

                if (comparison?["Benchmark"]?.GetValue<bool>() != true)
                {
                    return null;
                }

                return comparison["BenchmarkScore"]?.GetValue<double?>();
            }
            catch (Exception exception) when (exception is System.Text.Json.JsonException or InvalidOperationException or FormatException)
            {
                return null;
            }
        }
    }
}

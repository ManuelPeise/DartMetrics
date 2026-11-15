namespace Tools.BotTrainer
{
    /// <summary>Minimaler Parser: erstes Argument = Befehl, danach --name wert bzw. --flag.</summary>
    public sealed class Arguments
    {
        private readonly Dictionary<string, string?> _values = new(StringComparer.OrdinalIgnoreCase);

        public string Command { get; private init; } = "";

        public static Arguments Parse(string[] args)
        {
            var result = new Arguments { Command = args.Length > 0 ? args[0].ToLowerInvariant() : "" };

            for (var i = 1; i < args.Length; i++)
            {
                if (!args[i].StartsWith("--", StringComparison.Ordinal))
                {
                    continue;
                }

                var name = args[i][2..];
                var hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal);
                result._values[name] = hasValue ? args[++i] : null;
            }

            return result;
        }

        public int Int(string name, int fallback) =>
            _values.TryGetValue(name, out var value) && int.TryParse(value, out var number) && number > 0
                ? number
                : fallback;

        public bool Flag(string name) => _values.ContainsKey(name);
    }
}

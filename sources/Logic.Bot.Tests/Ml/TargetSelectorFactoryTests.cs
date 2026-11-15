using Data.DartContext.Entities.Bot;
using Logic.Bot.Ml;
using Logic.Bot.Models;
using Logic.Bot.Tactics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shared.Enums.Bot;
using Shared.Interfaces.Bot;
using Shared.Models.Bot.Ml;
using Shared.Models.Options;
using Xunit;

namespace Logic.Bot.Tests.Ml
{
    /// <summary>Ohne gültiges Modell muss immer das Regelwerk zurückkommen – Bots dürfen nie ausfallen.</summary>
    public class TargetSelectorFactoryTests : IDisposable
    {
        private readonly string _folder = Directory.CreateTempSubdirectory("dartbot-ml-").FullName;
        private readonly ITargetSelector _rules = new RuleBasedTargetSelector(new DefaultRandomSource(new Random(1)));

        public void Dispose() => Directory.Delete(_folder, recursive: true);

        private TargetSelectorFactory Factory(bool enabled = true) =>
            new(_rules, Options.Create(new DartBotOptions { Ml = new DartBotMlOptions { Enabled = enabled, Path = _folder } }),
                NullLogger<TargetSelectorFactory>.Instance);

        private static DartBotModelEntity Model(string storageKey = "models/missing.zip", int schema = TargetCandidateFeatures.FeatureSchemaVersion) => new()
        {
            Id = 7,
            ModelType = DartBotModelTypeEnum.TargetSelection,
            StorageKey = storageKey,
            Sha256 = new string('0', 64),
            FeatureSchemaVersion = schema,
            MlNetVersion = "4.0.2"
        };

        [Fact]
        public void NoModel_ReturnsRules() => Assert.Same(_rules, Factory().For(null));

        [Fact]
        public void MissingFile_ReturnsRules() => Assert.Same(_rules, Factory().For(Model()));

        [Fact]
        public void WrongSchema_ReturnsRules() => Assert.Same(_rules, Factory().For(Model(schema: 1)));

        [Fact]
        public void Disabled_ReturnsRules() => Assert.Same(_rules, Factory(enabled: false).For(Model()));

        [Fact]
        public void PathOutsideFolder_ReturnsRules() => Assert.Same(_rules, Factory().For(Model("../outside.zip")));

        [Fact]
        public void ShaMismatch_ReturnsRules()
        {
            Directory.CreateDirectory(Path.Combine(_folder, "models"));
            File.WriteAllText(Path.Combine(_folder, "models", "fake.zip"), "kein Modell");

            Assert.Same(_rules, Factory().For(Model("models/fake.zip")));
        }
    }
}

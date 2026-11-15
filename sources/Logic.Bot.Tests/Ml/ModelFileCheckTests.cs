using System.Security.Cryptography;
using System.Text;
using Data.DartContext.Entities.Bot;
using Logic.Bot.Ml;
using Shared.Enums.Bot;
using Shared.Models.Bot.Ml;
using Xunit;

namespace Logic.Bot.Tests.Ml
{
    public class ModelFileCheckTests : IDisposable
    {
        private readonly string _folder = Directory.CreateTempSubdirectory("dartbot-ml-check-").FullName;

        public void Dispose() => Directory.Delete(_folder, recursive: true);

        private DartBotModelEntity WriteModel(string content, string? sha = null, int schema = TargetCandidateFeatures.FeatureSchemaVersion)
        {
            Directory.CreateDirectory(Path.Combine(_folder, "models"));
            File.WriteAllText(Path.Combine(_folder, "models", "m.zip"), content);

            return new DartBotModelEntity
            {
                ModelType = DartBotModelTypeEnum.TargetSelection,
                StorageKey = "models/m.zip",
                Sha256 = sha ?? Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content))),
                FeatureSchemaVersion = schema,
                MlNetVersion = "4.0.2"
            };
        }

        [Fact]
        public void MatchingFile_IsOk() => Assert.Equal(DartBotModelFileStatusEnum.Ok, ModelFileCheck.Check(_folder, WriteModel("abc")));

        [Fact]
        public void ChangedFile_IsShaMismatch() =>
            Assert.Equal(DartBotModelFileStatusEnum.ShaMismatch, ModelFileCheck.Check(_folder, WriteModel("abc", sha: new string('0', 64))));

        [Fact]
        public void OldSchema_IsSchemaMismatch() =>
            Assert.Equal(DartBotModelFileStatusEnum.SchemaMismatch, ModelFileCheck.Check(_folder, WriteModel("abc", schema: 1)));

        [Fact]
        public void MissingFile_IsMissing()
        {
            var model = WriteModel("abc");
            model.StorageKey = "models/other.zip";

            Assert.Equal(DartBotModelFileStatusEnum.Missing, ModelFileCheck.Check(_folder, model));
        }

        [Theory]
        [InlineData("../outside.zip")]
        [InlineData("..")]
        public void PathOutsideFolder_IsRejected(string storageKey) => Assert.Null(ModelFileCheck.ResolvePath(_folder, storageKey));
    }
}

using Shared.Enums.Remote;
using Shared.Models.Remote;
using Web.App.Remote;

namespace Web.App.Tests.Remote
{
    /// <summary>Der Hub nimmt nur gültige Darts an (Kodierung wie DartRulesBase).</summary>
    public class RemoteCommandValidationTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(20, 3)]
        [InlineData(25, 1)]
        [InlineData(25, 2)]
        public void ValidDarts_AreAccepted(int segment, int multiplier) =>
            Assert.True(RemoteInputHub.IsValid(Dart(segment, multiplier)));

        [Theory]
        [InlineData(0, 1)]
        [InlineData(21, 1)]
        [InlineData(25, 3)]
        [InlineData(20, 0)]
        [InlineData(20, 4)]
        [InlineData(-1, 1)]
        public void InvalidDarts_AreRejected(int segment, int multiplier) =>
            Assert.False(RemoteInputHub.IsValid(Dart(segment, multiplier)));

        [Theory]
        [InlineData(RemoteCommandTypeEnum.Undo)]
        [InlineData(RemoteCommandTypeEnum.Submit)]
        public void UndoAndSubmit_AreAccepted(RemoteCommandTypeEnum type) =>
            Assert.True(RemoteInputHub.IsValid(new RemoteCommand { Type = type }));

        [Fact]
        public void UnknownType_IsRejected() =>
            Assert.False(RemoteInputHub.IsValid(new RemoteCommand { Type = (RemoteCommandTypeEnum)99 }));

        private static RemoteCommand Dart(int segment, int multiplier) =>
            new() { Type = RemoteCommandTypeEnum.Dart, Segment = segment, Multiplier = multiplier };
    }
}

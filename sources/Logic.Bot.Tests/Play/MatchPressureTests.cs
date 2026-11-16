using Logic.Bot.Play;
using Shared.Models.Bot;

namespace Logic.Bot.Tests.Play
{
    public class MatchPressureTests
    {
        private static readonly BotSkillProfile Nervous = new(0.9f, 0.8f, 1f, 0f, 0.5f) { SigmaMm = 10f, FavoriteDouble = 16, WeakDouble = 3 };

        [Fact]
        public void MatchDart_IncreasesScatter_DependingOnPressureResistance()
        {
            Assert.Equal(10f * (1f + BotVisitPlayer.MaxMatchPressurePenalty), BotVisitPlayer.WithMatchPressure(Nervous, 1f).SigmaMm!.Value, 3);
            Assert.Equal(10f, BotVisitPlayer.WithMatchPressure(Nervous with { PressureResistance = 1f }, 1f).SigmaMm!.Value, 3);
            Assert.Same(Nervous, BotVisitPlayer.WithMatchPressure(Nervous, 0f));
        }

        [Fact]
        public void FavoriteAndWeakDouble_ChangeScatter()
        {
            Assert.Equal(10f * BotSkillProfile.FavoriteDoubleFactor, BotVisitPlayer.WithFieldFactor(Nervous, DartTarget.Double(16)).SigmaMm!.Value, 3);
            Assert.Equal(10f * BotSkillProfile.WeakDoubleFactor, BotVisitPlayer.WithFieldFactor(Nervous, DartTarget.Double(3)).SigmaMm!.Value, 3);
            Assert.Equal(10f, BotVisitPlayer.WithFieldFactor(Nervous, DartTarget.Triple(16)).SigmaMm!.Value, 3);
        }
    }
}

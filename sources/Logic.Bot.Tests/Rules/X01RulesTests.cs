using Logic.Shared.Rules;
using Shared.Enums.X01;

namespace Logic.Bot.Tests.Rules
{
    /// <summary>X01Rules liegt in Logic.Shared, ist aber die Grundlage der Bot-Logik.</summary>
    public class X01RulesTests
    {
        private readonly X01Rules _rules = new();

        [Fact]
        public void DoubleOut_RestOne_IsBust()
        {
            Assert.True(_rules.IsBust(41, 40, 20, 2, X01OutModeEnum.Double));
        }

        [Fact]
        public void StraightOut_RestOne_IsNotBust()
        {
            Assert.False(_rules.IsBust(41, 40, 20, 2, X01OutModeEnum.Straight));
        }

        [Fact]
        public void DoubleOut_TripleFinish_IsBust()
        {
            Assert.True(_rules.IsBust(60, 60, 20, 3, X01OutModeEnum.Double));
        }

        [Fact]
        public void StraightOut_TripleFinish_IsCheckout()
        {
            Assert.True(_rules.IsCheckout(60, 60, 20, 3, X01OutModeEnum.Straight));
        }

        [Fact]
        public void StraightOut_SingleFinish_IsCheckout()
        {
            Assert.True(_rules.IsCheckout(20, 20, 20, 1, X01OutModeEnum.Straight));
        }

        [Fact]
        public void StraightOut_RestOne_HasCheckoutWithSingleOne()
        {
            var routes = _rules.GetCheckoutSuggestions(1, 1, X01OutModeEnum.Straight);

            var route = Assert.Single(routes);
            Assert.Equal((1, 1), (route.Darts[0].Segment, route.Darts[0].Multiplier));
        }

        [Fact]
        public void StraightOut_180_IsCheckableWithThreeDarts()
        {
            Assert.NotEmpty(_rules.GetCheckoutSuggestions(180, 3, X01OutModeEnum.Straight));
            Assert.Empty(_rules.GetCheckoutSuggestions(180, 3, X01OutModeEnum.Double));
        }

        [Fact]
        public void Bull_IsFinishingDart()
        {
            Assert.True(_rules.IsFinishingDart(25, 2, X01OutModeEnum.Double));
        }

        [Fact]
        public void DoubleIn_CountsFromOpeningDart()
        {
            var darts = new[] { (20, 3), (20, 2), (20, 1) };

            Assert.Equal(60, _rules.CountedScore(darts, isOpened: false, X01InModeEnum.Double));
        }

        [Fact]
        public void DoubleIn_NoDouble_CountsNothing()
        {
            var darts = new[] { (20, 3), (19, 3), (18, 1) };

            Assert.Equal(0, _rules.CountedScore(darts, isOpened: false, X01InModeEnum.Double));
        }

        [Fact]
        public void StraightIn_CountsEverything()
        {
            var darts = new[] { (20, 3), (19, 3), (18, 1) };

            Assert.Equal(135, _rules.CountedScore(darts, isOpened: false, X01InModeEnum.Straight));
        }
    }
}

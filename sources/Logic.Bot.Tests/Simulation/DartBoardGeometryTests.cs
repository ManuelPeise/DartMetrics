using Logic.Bot.Simulation;
using Shared.Models.Bot;

namespace Logic.Bot.Tests.Simulation
{
    public class DartBoardGeometryTests
    {
        public static TheoryData<int, int> AllTargets()
        {
            var data = new TheoryData<int, int>();

            for (var segment = 1; segment <= 20; segment++)
            {
                data.Add(segment, 1);
                data.Add(segment, 2);
                data.Add(segment, 3);
            }

            data.Add(25, 1);
            data.Add(25, 2);
            return data;
        }

        [Theory]
        [MemberData(nameof(AllTargets))]
        public void AimPoint_ResolvesToItsOwnTarget(int segment, int multiplier)
        {
            var target = new DartTarget(segment, multiplier);

            var resolved = DartBoardGeometry.Resolve(DartBoardGeometry.GetAimPoint(target));

            Assert.Equal(target, resolved);
        }

        [Fact]
        public void Center_IsBull()
        {
            Assert.Equal(new DartTarget(25, 2), DartBoardGeometry.Resolve(new BoardPoint(0, 0)));
        }

        [Fact]
        public void Twenty_IsAtTheTop()
        {
            Assert.Equal(new DartTarget(20, 1), DartBoardGeometry.Resolve(new BoardPoint(0, 60)));
        }

        [Fact]
        public void Six_IsOnTheRight()
        {
            Assert.Equal(new DartTarget(6, 1), DartBoardGeometry.Resolve(new BoardPoint(60, 0)));
        }

        [Fact]
        public void OutsideDoubleRing_IsMiss()
        {
            var resolved = DartBoardGeometry.Resolve(new BoardPoint(0, DartBoardGeometry.DoubleOuterRadius + 1));

            Assert.True(resolved.IsMiss);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(21, 1)]
        [InlineData(20, 4)]
        public void GetAimPoint_InvalidTarget_Throws(int segment, int multiplier)
        {
            Assert.Throws<ArgumentException>(() => DartBoardGeometry.GetAimPoint(new DartTarget(segment, multiplier)));
        }
    }
}

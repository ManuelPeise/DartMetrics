using Web.App.Remote;

namespace Web.App.Tests.Remote
{
    /// <summary>Pro Benutzer genau eine aktive Spielseite, beliebig viele Handys, Benutzer getrennt.</summary>
    public class RemoteConnectionRegistryTests
    {
        [Fact]
        public void NewDisplay_ReplacesPrevious_AndReturnsIt()
        {
            var registry = new RemoteConnectionRegistry();

            Assert.Null(registry.SetDisplay("7", "tab-1"));
            Assert.Equal("tab-1", registry.SetDisplay("7", "tab-2"));
            Assert.Equal("tab-2", registry.GetDisplay("7"));
            Assert.False(registry.IsDisplay("7", "tab-1"));
        }

        [Fact]
        public void SameDisplayAgain_ReturnsNoPrevious()
        {
            var registry = new RemoteConnectionRegistry();
            registry.SetDisplay("7", "tab-1");

            Assert.Null(registry.SetDisplay("7", "tab-1"));
        }

        [Fact]
        public void RemoveDisplay_OnlyForTheActiveOne()
        {
            var registry = new RemoteConnectionRegistry();
            registry.SetDisplay("7", "tab-1");
            registry.SetDisplay("7", "tab-2");

            Assert.False(registry.RemoveDisplay("7", "tab-1"));
            Assert.Equal("tab-2", registry.GetDisplay("7"));
            Assert.True(registry.RemoveDisplay("7", "tab-2"));
            Assert.Null(registry.GetDisplay("7"));
        }

        [Fact]
        public void Controllers_AreTrackedPerUser()
        {
            var registry = new RemoteConnectionRegistry();
            registry.AddController("7", "phone-a", "Pixel");
            registry.AddController("7", "phone-b", "iPhone");
            registry.AddController("8", "phone-c", "Galaxy");

            Assert.Equal(new[] { "Pixel", "iPhone" }.Order(StringComparer.Ordinal), registry.GetControllerNames("7").Order(StringComparer.Ordinal));
            Assert.Equal(new[] { "Galaxy" }, registry.GetControllerNames("8"));

            Assert.True(registry.RemoveController("7", "phone-a"));
            Assert.False(registry.RemoveController("7", "phone-a"));
            Assert.Equal(new[] { "iPhone" }, registry.GetControllerNames("7"));
        }

        [Fact]
        public void UsersDoNotSeeEachOther()
        {
            var registry = new RemoteConnectionRegistry();
            registry.SetDisplay("7", "tab-1");

            Assert.Null(registry.GetDisplay("8"));
            Assert.False(registry.IsDisplay("8", "tab-1"));
            Assert.False(registry.RemoveDisplay("8", "tab-1"));
            Assert.Empty(registry.GetControllerNames("8"));
        }
    }
}

namespace Web.App.Tests.Remote
{
    /// <summary>Kopplung: Code einmalig und kurz gültig, Geräte-Token fälschungssicher und an Benutzer/Sitzung gebunden.</summary>
    public class RemotePairingServiceTests
    {
        [Fact]
        public async Task Redeem_ValidCode_ReturnsTokenBoundToUserAndSession()
        {
            var pairing = RemoteTestHelpers.NewPairing();
            var (code, _) = await pairing.CreatePairingAsync("7", "session-1", "max@example.org", default);

            var result = await pairing.RedeemAsync(code, "Pixel 8", default);

            Assert.NotNull(result);
            var ticket = pairing.ReadToken(result.Value.Token);
            Assert.NotNull(ticket);
            Assert.Equal("7", ticket.UserId);
            Assert.Equal("session-1", ticket.SessionId);
            Assert.Equal("Pixel 8", ticket.DeviceName);
            Assert.Equal("max@example.org", result.Value.UserName);
        }

        [Fact]
        public async Task Redeem_SameCodeTwice_SecondFails()
        {
            var pairing = RemoteTestHelpers.NewPairing();
            var (code, _) = await pairing.CreatePairingAsync("7", "session-1", "max", default);

            Assert.NotNull(await pairing.RedeemAsync(code, null, default));
            Assert.Null(await pairing.RedeemAsync(code, null, default));
        }

        [Fact]
        public async Task Redeem_ExpiredCode_Fails()
        {
            var pairing = RemoteTestHelpers.NewPairing(pairingCodeSeconds: 1);
            var (code, _) = await pairing.CreatePairingAsync("7", "session-1", "max", default);

            await Task.Delay(TimeSpan.FromMilliseconds(1500));

            Assert.Null(await pairing.RedeemAsync(code, null, default));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("unknown-code")]
        public async Task Redeem_UnknownOrEmptyCode_Fails(string code)
        {
            var pairing = RemoteTestHelpers.NewPairing();

            Assert.Null(await pairing.RedeemAsync(code, null, default));
        }

        [Fact]
        public async Task Redeem_TooLongCode_IsRejectedWithoutLookup()
        {
            var pairing = RemoteTestHelpers.NewPairing();

            Assert.Null(await pairing.RedeemAsync(new string('a', 65), null, default));
        }

        [Fact]
        public async Task CreatePairing_CodesAreUniqueAndNotGuessable()
        {
            var pairing = RemoteTestHelpers.NewPairing();
            var codes = new HashSet<string>();

            for (var i = 0; i < 50; i++)
            {
                var (code, _) = await pairing.CreatePairingAsync("7", "s", "max", default);
                Assert.True(code.Length >= 32, "Code muss mindestens 24 Zufallsbytes enthalten.");
                Assert.True(codes.Add(code));
            }
        }

        [Fact]
        public async Task DeviceName_IsDefaultedAndTruncated()
        {
            var pairing = RemoteTestHelpers.NewPairing();

            var (code1, _) = await pairing.CreatePairingAsync("7", "s", "max", default);
            var unnamed = pairing.ReadToken((await pairing.RedeemAsync(code1, "  ", default))!.Value.Token);

            var (code2, _) = await pairing.CreatePairingAsync("7", "s", "max", default);
            var longName = pairing.ReadToken((await pairing.RedeemAsync(code2, new string('x', 100), default))!.Value.Token);

            Assert.Equal("Handy", unnamed!.DeviceName);
            Assert.Equal(60, longName!.DeviceName.Length);
        }

        [Fact]
        public async Task ReadToken_TamperedOrForeignToken_IsRejected()
        {
            var pairing = RemoteTestHelpers.NewPairing();
            var (code, _) = await pairing.CreatePairingAsync("7", "s", "max", default);
            var token = (await pairing.RedeemAsync(code, null, default))!.Value.Token;

            // Anderer Schlüsselring (z. B. fremder Server) kann den Token nicht lesen.
            var foreign = RemoteTestHelpers.NewPairing();

            Assert.Null(pairing.ReadToken(token[..^4] + "AAAA"));
            Assert.Null(pairing.ReadToken("not-a-token"));
            Assert.Null(pairing.ReadToken(null));
            Assert.Null(pairing.ReadToken(new string('a', 5000)));
            Assert.Null(foreign.ReadToken(token));
        }

        [Fact]
        public async Task Token_ExpiresAfterConfiguredHours()
        {
            var pairing = RemoteTestHelpers.NewPairing(deviceTokenHours: 12);
            var (code, _) = await pairing.CreatePairingAsync("7", "s", "max", default);

            var result = await pairing.RedeemAsync(code, null, default);

            var expected = DateTime.UtcNow.AddHours(12);
            Assert.InRange(result!.Value.ExpiresAtUtc, expected.AddMinutes(-1), expected.AddMinutes(1));
        }
    }
}

using Data.DartContext;
using Data.DartContext.Entities.Bot;
using Logic.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Enums.User;
using Shared.Interfaces.Administration;
using Shared.Interfaces.Bot;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Bot;

namespace Logic.Integration.Tests.Bot
{
    /// <summary>
    /// Zwei Anfragen ändern denselben Bot: die zweite scheitert am ConcurrencyStamp,
    /// MergeConcurrentBotUpdatesAsync rechnet ihre Änderung auf den neuen Stand -> keine XP gehen verloren.
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public class BotConcurrencyTests : IAsyncLifetime
    {
        private readonly MySqlFixture _fixture;
        private TestHost _host = null!;

        public BotConcurrencyTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync() => _host = await TestHost.CreateAsync(_fixture);

        public async Task DisposeAsync() => await _host.DisposeAsync();

        [Fact]
        public async Task ConcurrentXpChanges_AreMerged()
        {
            var adminId = await _host.CreateUserAsync();
            _host.Claims.SignIn(adminId, UserRoleEnum.Admin);
            var botId = await _host.RunAsync<IDartBotAdministration, int>(a =>
                a.CreateBotAsync(new CreateDartBotModel { Name = "Concurrency", Alias = "CC" }));

            await using var first = _host.CreateScope();
            await using var second = _host.CreateScope();

            var firstUnitOfWork = first.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var secondUnitOfWork = second.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var firstBot = await firstUnitOfWork.DartBotUnitOfWork.DartBotRepository.GetByIdAsync(botId);
            var secondBot = await secondUnitOfWork.DartBotUnitOfWork.DartBotRepository.GetByIdAsync(botId);

            secondBot.ExperiencePoints += 100;
            await secondUnitOfWork.SaveChangesAsync();

            firstBot.ExperiencePoints += 50;
            var learning = first.ServiceProvider.GetRequiredService<IDartBotLearningService>();

            var exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => firstUnitOfWork.SaveChangesAsync());
            Assert.True(await learning.MergeConcurrentBotUpdatesAsync(exception));
            await firstUnitOfWork.SaveChangesAsync();

            var xp = await _host.DbAsync(db => db.Set<DartBotEntity>().Where(b => b.Id == botId).Select(b => b.ExperiencePoints).SingleAsync());
            Assert.Equal(150L, xp);
        }
    }
}

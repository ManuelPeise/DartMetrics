using Data.DartContext;
using Data.DartContext.Entities;
using Data.DartContext.Entities.Bot;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Interfaces.UnitsOfWork;

namespace Logic.Shared.UnitsOfWork
{
    public class UnitOfWork: IUnitOfWork
    {
        private readonly HttpContext? _httpContext;
        private readonly DartDatabaseContext _dartDatabaseContext;

        public IUserUnitOfWork UserUnitOfWork { get; }
        public IDartTrainingUnitOfWork DartTrainingUnitOfWork { get; }
        public IDartBotUnitOfWork DartBotUnitOfWork { get; }
        public ITournamentUnitOfWork TournamentUnitOfWork { get; }


        public UnitOfWork(DartDatabaseContext dartDatabaseContext, IHttpContextAccessor httpContextAccessor)
        {
            _dartDatabaseContext = dartDatabaseContext;
            _httpContext = httpContextAccessor.HttpContext;

            UserUnitOfWork = new UserUnitOfWork(_dartDatabaseContext);
            DartTrainingUnitOfWork = new DartTrainingUnitOfWork(_dartDatabaseContext);
            DartBotUnitOfWork = new DartBotUnitOfWork(_dartDatabaseContext);
            TournamentUnitOfWork = new TournamentUnitOfWork(_dartDatabaseContext);
        }

        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            EnsureImmutableEntitiesUnchanged();

            StampAuditFields(GetCurrentUserName());

            return await _dartDatabaseContext.SaveChangesAsync(cancellationToken) > 0;
        }

        private void EnsureImmutableEntitiesUnchanged()
        {
            foreach (var entry in _dartDatabaseContext.ChangeTracker.Entries())
            {
                if (entry.Entity is DartBotProfileVersionEntity
                    && entry.State is EntityState.Modified or EntityState.Deleted)
                {
                    throw new InvalidOperationException(
                        $"{entry.Entity.GetType().Name} (Id {((AEntityBase)entry.Entity).Id}) ist unveränderlich und darf nicht geändert oder gelöscht werden.");
                }
            }
        }

        private void StampAuditFields(string currentUser)
        {
            var now = DateTime.UtcNow;

            // Neuer Nebenläufigkeitsstempel bei jeder Änderung (die WHERE-Klausel prüft den alten Wert).
            foreach (var entry in _dartDatabaseContext.ChangeTracker.Entries<IConcurrencyStamped>())
            {
                if (entry.State is EntityState.Added or EntityState.Modified)
                {
                    entry.Entity.ConcurrencyStamp = Guid.NewGuid();
                }
            }

            foreach (var entry in _dartDatabaseContext.ChangeTracker.Entries<AEntityBase>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = currentUser;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = currentUser;
                }
            }
        }

        private string GetCurrentUserName()
        {
            var userName = _httpContext?.User.Identity?.Name ?? "System";

            return userName;
        }
    }
}

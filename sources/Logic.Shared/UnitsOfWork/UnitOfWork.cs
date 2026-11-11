using Data.DartContext;
using Data.DartContext.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Interfaces.UnitsOfWork;

namespace Logic.Shared.UnitsOfWork
{
    public class UnitOfWork: IUnitOfWork
    {
        private readonly HttpContext _httpContext;
        private readonly DartDatabaseContext _dartDatabaseContext;

        public IUserUnitOfWork UserUnitOfWork { get; }

        public UnitOfWork(DartDatabaseContext dartDatabaseContext, IHttpContextAccessor httpContextAccessor)
        {
            _dartDatabaseContext = dartDatabaseContext;
            _httpContext = httpContextAccessor.HttpContext;

            UserUnitOfWork = new UserUnitOfWork(_dartDatabaseContext);
        }

        public Task<bool> SaveChangesAsync()
        {
            var currentUserName = GetCurrentUserName();

            StampAuditFields(currentUserName);

            var savedRows = _dartDatabaseContext.SaveChangesAsync();

            return savedRows.ContinueWith(task => task.Result > 0);
        }

        private void StampAuditFields(string currentUser)
        {
            var now = DateTime.UtcNow;

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

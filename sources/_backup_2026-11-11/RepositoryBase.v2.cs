using Data.DartContext;
using Data.DartContext.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Interfaces.Repositories;
using System.Linq.Expressions;

namespace Logic.Shared.Repositories
{
    public class RepositoryBase<TEntity> : IRepositoryBase<TEntity> where TEntity : AEntityBase
    {
        protected readonly DartDatabaseContext Context;
        protected readonly DbSet<TEntity> DbSet;

        public RepositoryBase(DartDatabaseContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            DbSet = context.Set<TEntity>();
        }

        protected virtual IQueryable<TEntity> Query(bool asNoTracking)
            => asNoTracking ? DbSet.AsNoTracking() : DbSet;

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(bool asNoTracking = false, CancellationToken cancellationToken = default)
            => await Query(asNoTracking).ToListAsync(cancellationToken);

        public virtual async Task<TEntity> GetByIdAsync(int id, bool asNoTracking = false, CancellationToken cancellationToken = default)
        {
            var entity = await Query(asNoTracking).FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

            return entity ?? throw new KeyNotFoundException($"{typeof(TEntity).Name} with id {id} was not found.");
        }

        public virtual Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate, bool asNoTracking = false, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return Query(asNoTracking).Where(predicate).ToListAsync(cancellationToken);
        }

        public virtual Task<TEntity?> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool asNoTracking = false, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return Query(asNoTracking).FirstOrDefaultAsync(predicate, cancellationToken);
        }

        public virtual Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return DbSet.AnyAsync(predicate, cancellationToken);
        }

        public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await DbSet.AddAsync(entity, cancellationToken);
        }

        public virtual Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entities);
            return DbSet.AddRangeAsync(entities, cancellationToken);
        }

        public virtual void Update(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            DbSet.Update(entity);
        }

        public virtual void Delete(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            DbSet.Remove(entity);
        }

        public virtual void DeleteRange(IEnumerable<TEntity> entities)
        {
            ArgumentNullException.ThrowIfNull(entities);
            DbSet.RemoveRange(entities);
        }
    }
}

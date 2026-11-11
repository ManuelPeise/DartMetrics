using Data.DartContext.Entities;
using System.Linq.Expressions;

namespace Shared.Interfaces.Repositories
{
    public interface IRepositoryBase<TEntity> where TEntity : AEntityBase
    {
        /// <summary>
        /// All read methods accept an optional <c>include</c> to load navigation properties, e.g.
        /// <c>include: q => q.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)</c>
        /// </summary>
        Task<IEnumerable<TEntity>> GetAllAsync(bool asNoTracking = false, Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null, CancellationToken cancellationToken = default);

        Task<TEntity> GetByIdAsync(int id, bool asNoTracking = false, Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null, CancellationToken cancellationToken = default);

        Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate, bool asNoTracking = false, Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null, CancellationToken cancellationToken = default);
        Task<TEntity?> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool asNoTracking = false, Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null, CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        void Update(TEntity entity);
        void Delete(TEntity entity);
        void DeleteRange(IEnumerable<TEntity> entities);
    }
}

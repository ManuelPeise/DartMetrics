using Data.DartContext.Entities;
using System.Linq.Expressions;

namespace Shared.Interfaces.Repositories
{
    public interface IRepositoryBase<TEntity> where TEntity : AEntityBase
    {
        Task<IEnumerable<TEntity>> GetAllAsync(bool asNoTracking = false, CancellationToken cancellationToken = default);

        Task<TEntity> GetByIdAsync(int id, bool asNoTracking = false, CancellationToken cancellationToken = default);

        Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate, bool asNoTracking = false, CancellationToken cancellationToken = default);
        Task<TEntity?> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool asNoTracking = false, CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        void Update(TEntity entity);
        void Delete(TEntity entity);
        void DeleteRange(IEnumerable<TEntity> entities);
    }
}

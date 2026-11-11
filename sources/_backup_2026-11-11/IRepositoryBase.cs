using Data.DartContext.Entities;
using System.Linq.Expressions;

namespace Shared.Interfaces.Repositories
{
    public interface IRepositoryBase<TEntity> where TEntity : AEntityBase
    {
        Task<IEnumerable<TEntity>> GetAllAsync(bool asNoTracking = false);
        Task<TEntity> GetByIdAsync(int id, bool asNoTracking = false);
        Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate, bool asNoTracking = false);
        Task<TEntity?> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool asNoTracking = false);
        Task AddAsync(TEntity entity);
        Task AddRangeAsync(IEnumerable<TEntity> entities);
        void Update(TEntity entity);
        void Delete(TEntity entity);
        void DeleteRange(IEnumerable<TEntity> entities);

    }
}

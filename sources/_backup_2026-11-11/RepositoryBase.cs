using Data.DartContext;
using Data.DartContext.Entities;
using Shared.Interfaces.Repositories;

namespace Logic.Shared.Repositories
{
    public class RepositoryBase<TEntity> : IRepositoryBase<TEntity> where TEntity : AEntityBase
    {
        private readonly DartDatabaseContext _context;

        public RepositoryBase(DartDatabaseContext context)
        {
            _context = context;
        }


    }
}

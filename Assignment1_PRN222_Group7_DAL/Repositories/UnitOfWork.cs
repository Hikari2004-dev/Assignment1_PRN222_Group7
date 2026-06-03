using Assignment1_PRN222_Group7_DAL.Context;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7_DAL.Repositories
{
    /// <summary>Unit of Work implementation coordinating generic repositories with ApplicationDbContext.</summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly ConcurrentDictionary<Type, object> _repositories = new();

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IGenericRepository<T> GetRepository<T>() where T : class
        {
            return (IGenericRepository<T>)_repositories.GetOrAdd(
                typeof(T),
                _ => new GenericRepository<T>(_context)
            );
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

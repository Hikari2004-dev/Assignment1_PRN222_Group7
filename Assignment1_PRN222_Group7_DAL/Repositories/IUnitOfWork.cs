using System;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7_DAL.Repositories
{
    /// <summary>Unit of Work interface to manage database transactions and repositories.</summary>
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<T> GetRepository<T>() where T : class;
        Task<int> SaveChangesAsync();
    }
}

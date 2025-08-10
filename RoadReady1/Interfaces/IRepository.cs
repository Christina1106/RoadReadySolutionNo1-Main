using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RoadReady1.Interfaces
{
    /// <summary>Generic async CRUD repository.</summary>
    public interface IRepository<K, T> where T : class
    {
        Task<T> AddAsync(T entity);
        Task<T> UpdateAsync(K key, T entity);
        Task<T> DeleteAsync(K key);
        Task<T> GetByIdAsync(K key);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> FindAsync(Expression<Func<T, bool>> predicate);
    }
}


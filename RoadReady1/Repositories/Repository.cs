using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RoadReady1.Context;
using RoadReady1.Exceptions;
using RoadReady1.Interfaces;

namespace RoadReady1.Repositories
{
    /// <summary>
    /// EF Core implementation of IRepository&lt;K,T&gt; with async methods.
    /// </summary>
    public class Repository<K, T> : IRepository<K, T> where T : class
    {
        protected readonly RoadReadyDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(RoadReadyDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<T> UpdateAsync(K key, T entity)
        {
            var existing = await GetByIdAsync(key);
            _context.Entry(existing).CurrentValues.SetValues(entity);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<T> DeleteAsync(K key)
        {
            var existing = await GetByIdAsync(key);
            _dbSet.Remove(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<T> GetByIdAsync(K key)
        {
            var entity = await _dbSet.FindAsync(key);
            if (entity == null)
                throw new NotFoundException($"{typeof(T).Name} with key '{key}' not found.");
            return entity;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }
    }
}

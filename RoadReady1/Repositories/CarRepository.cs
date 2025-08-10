// File: Repositories/CarRepository.cs
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RoadReady1.Context;
using RoadReady1.Interfaces;
using RoadReady1.Models;

namespace RoadReady1.Repositories
{
    public class CarRepository : RepositoryDB<int, Car>, IRepository<int, Car>
    {
        public CarRepository(RoadReadyDbContext context) : base(context) { }

        public override async Task<IEnumerable<Car>> GetAllAsync()
            => await _context.Cars.AsNoTracking().ToListAsync();

        public override async Task<Car?> GetByIdAsync(int key)
            => await _context.Cars.FirstOrDefaultAsync(c => c.CarId == key);

        public override async Task<Car?> FindAsync(Expression<Func<Car, bool>> predicate)
            => await _context.Cars.AsNoTracking().FirstOrDefaultAsync(predicate);
    }
}

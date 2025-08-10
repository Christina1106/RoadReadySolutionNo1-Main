using Microsoft.EntityFrameworkCore;
using RoadReady1.Context;
using RoadReady1.Interfaces;
using RoadReady1.Models;

namespace RoadReady1.Repositories
{
    public class CarStatusRepository : RepositoryDB<int, CarStatus>
    {
        public CarStatusRepository(RoadReadyDbContext ctx) : base(ctx) { }

        public override async Task<IEnumerable<CarStatus>> GetAllAsync()
            => await _context.CarStatuses.AsNoTracking().ToListAsync();

        public override async Task<CarStatus?> GetByIdAsync(int id)
            => await _context.CarStatuses.AsNoTracking()
                   .FirstOrDefaultAsync(s => s.StatusId == id);

        public override async Task<CarStatus?> FindAsync(System.Linq.Expressions.Expression<Func<CarStatus, bool>> predicate)
            => await _context.CarStatuses.AsNoTracking().FirstOrDefaultAsync(predicate);
    }
}

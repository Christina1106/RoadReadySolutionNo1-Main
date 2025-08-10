using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RoadReady1.Context;
using RoadReady1.Interfaces;
using RoadReady1.Models;

namespace RoadReady1.Repositories
{
    public class LocationRepository : RepositoryDB<int, Location>, IRepository<int, Location>
    {
        public LocationRepository(RoadReadyDbContext ctx) : base(ctx) { }

        public override async Task<IEnumerable<Location>> GetAllAsync()
            => await _context.Locations.AsNoTracking().ToListAsync();

        public override async Task<Location?> GetByIdAsync(int key)
            => await _context.Locations.FirstOrDefaultAsync(l => l.LocationId == key);

        public override async Task<Location?> FindAsync(Expression<Func<Location, bool>> predicate)
            => await _context.Locations.AsNoTracking().FirstOrDefaultAsync(predicate);
    }
}

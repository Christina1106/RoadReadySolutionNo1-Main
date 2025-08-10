using Microsoft.EntityFrameworkCore;
using RoadReady1.Context;
using RoadReady1.Interfaces;
using RoadReady1.Models;

namespace RoadReady1.Repositories
{
    public class RoleRepository : RepositoryDB<int, Role>
    {
        public RoleRepository(RoadReadyDbContext ctx) : base(ctx) { }

        public override async Task<IEnumerable<Role>> GetAllAsync()
            => await _context.Roles.AsNoTracking().ToListAsync();

        public override async Task<Role?> GetByIdAsync(int id)
            => await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == id);

        public override async Task<Role?> FindAsync(System.Linq.Expressions.Expression<Func<Role, bool>> predicate)
            => await _context.Roles.AsNoTracking().FirstOrDefaultAsync(predicate);
    }
}

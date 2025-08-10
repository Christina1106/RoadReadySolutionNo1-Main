// File: Repositories/MaintenanceRequestRepository.cs
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RoadReady1.Context;
using RoadReady1.Interfaces;
using RoadReady1.Models;

namespace RoadReady1.Repositories
{
    public class MaintenanceRequestRepository : RepositoryDB<int, MaintenanceRequest>, IRepository<int, MaintenanceRequest>
    {
        public MaintenanceRequestRepository(RoadReadyDbContext ctx) : base(ctx) { }

        public override async Task<IEnumerable<MaintenanceRequest>> GetAllAsync()
            => await _context.MaintenanceRequests.AsNoTracking().ToListAsync();

        public override async Task<MaintenanceRequest?> GetByIdAsync(int key)
            => await _context.MaintenanceRequests.FirstOrDefaultAsync(r => r.RequestId == key);

        public override async Task<MaintenanceRequest?> FindAsync(Expression<Func<MaintenanceRequest, bool>> predicate)
            => await _context.MaintenanceRequests.AsNoTracking().FirstOrDefaultAsync(predicate);
    }
}

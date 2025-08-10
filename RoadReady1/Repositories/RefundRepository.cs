using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RoadReady1.Context;
using RoadReady1.Interfaces;
using RoadReady1.Models;

namespace RoadReady1.Repositories
{
    public class RefundRepository : RepositoryDB<int, Refund>, IRepository<int, Refund>
    {
        public RefundRepository(RoadReadyDbContext ctx) : base(ctx) { }

        public override async Task<IEnumerable<Refund>> GetAllAsync()
            => await _context.Refunds.AsNoTracking().ToListAsync();

        public override async Task<Refund?> GetByIdAsync(int key)
            => await _context.Refunds.FirstOrDefaultAsync(r => r.RefundId == key);

        public override async Task<Refund?> FindAsync(Expression<Func<Refund, bool>> predicate)
            => await _context.Refunds.AsNoTracking().FirstOrDefaultAsync(predicate);
    }
}

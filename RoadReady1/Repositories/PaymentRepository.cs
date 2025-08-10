using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RoadReady1.Context;
using RoadReady1.Interfaces;
using RoadReady1.Models;

namespace RoadReady1.Repositories
{
    public class PaymentRepository : RepositoryDB<int, Payment>, IRepository<int, Payment>
    {
        public PaymentRepository(RoadReadyDbContext ctx) : base(ctx) { }

        public override async Task<IEnumerable<Payment>> GetAllAsync()
            => await _context.Payments.AsNoTracking().ToListAsync();

        public override async Task<Payment?> GetByIdAsync(int key)
            => await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == key);

        public override async Task<Payment?> FindAsync(Expression<Func<Payment, bool>> predicate)
            => await _context.Payments.AsNoTracking().FirstOrDefaultAsync(predicate);
    }
}

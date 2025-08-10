using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RoadReady1.Context;
using RoadReady1.Interfaces;
using RoadReady1.Models;

namespace RoadReady1.Repositories
{
    public class PaymentMethodRepository : RepositoryDB<int, PaymentMethod>, IRepository<int, PaymentMethod>
    {
        public PaymentMethodRepository(RoadReadyDbContext ctx) : base(ctx) { }

        public override async Task<IEnumerable<PaymentMethod>> GetAllAsync()
            => await _context.PaymentMethods.AsNoTracking().ToListAsync();

        public override async Task<PaymentMethod?> GetByIdAsync(int key)
            => await _context.PaymentMethods.FirstOrDefaultAsync(m => m.MethodId == key);

        public override async Task<PaymentMethod?> FindAsync(Expression<Func<PaymentMethod, bool>> predicate)
            => await _context.PaymentMethods.AsNoTracking().FirstOrDefaultAsync(predicate);
    }
}

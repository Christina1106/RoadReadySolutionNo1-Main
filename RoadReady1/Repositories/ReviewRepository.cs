using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RoadReady1.Context;
using RoadReady1.Interfaces;
using RoadReady1.Models;

namespace RoadReady1.Repositories
{
    public class ReviewRepository : RepositoryDB<int, Review>, IRepository<int, Review>
    {
        public ReviewRepository(RoadReadyDbContext ctx) : base(ctx) { }

        public override async Task<IEnumerable<Review>> GetAllAsync()
            => await _context.Reviews.AsNoTracking().ToListAsync();

        public override async Task<Review?> GetByIdAsync(int key)
            => await _context.Reviews.FirstOrDefaultAsync(r => r.ReviewId == key);

        public override async Task<Review?> FindAsync(Expression<Func<Review, bool>> predicate)
            => await _context.Reviews.AsNoTracking().FirstOrDefaultAsync(predicate);
    }
}

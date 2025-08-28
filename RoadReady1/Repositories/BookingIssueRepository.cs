using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RoadReady1.Context;
using RoadReady1.Interfaces;
using RoadReady1.Models;

namespace RoadReady1.Repositories
{
    public class BookingIssueRepository : RepositoryDB<int, BookingIssue>, IRepository<int, BookingIssue>
    {
        public BookingIssueRepository(RoadReadyDbContext ctx) : base(ctx) { }

        public override async Task<IEnumerable<BookingIssue>> GetAllAsync()
            => await _context.BookingIssues.AsNoTracking().ToListAsync();

        public override async Task<BookingIssue?> GetByIdAsync(int key)
            => await _context.BookingIssues.FirstOrDefaultAsync(i => i.IssueId == key);

        public override async Task<BookingIssue?> FindAsync(Expression<Func<BookingIssue, bool>> predicate)
            => await _context.BookingIssues.AsNoTracking().FirstOrDefaultAsync(predicate);
    }
}

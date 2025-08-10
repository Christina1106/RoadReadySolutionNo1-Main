using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RoadReady1.Context;
using RoadReady1.Interfaces;
using RoadReady1.Models;

namespace RoadReady1.Repositories
{
    public class BookingStatusRepository : RepositoryDB<int, BookingStatus>, IRepository<int, BookingStatus>
    {
        public BookingStatusRepository(RoadReadyDbContext ctx) : base(ctx) { }

        public override async Task<IEnumerable<BookingStatus>> GetAllAsync()
            => await _context.BookingStatuses.AsNoTracking().ToListAsync();

        public override async Task<BookingStatus?> GetByIdAsync(int key)
            => await _context.BookingStatuses.FirstOrDefaultAsync(s => s.StatusId == key);

        public override async Task<BookingStatus?> FindAsync(Expression<Func<BookingStatus, bool>> predicate)
            => await _context.BookingStatuses.AsNoTracking().FirstOrDefaultAsync(predicate);
    }
}

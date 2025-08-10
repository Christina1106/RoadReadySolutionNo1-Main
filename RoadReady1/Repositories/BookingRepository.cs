using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RoadReady1.Context;
using RoadReady1.Interfaces;
using RoadReady1.Models;

namespace RoadReady1.Repositories
{
    public class BookingRepository : RepositoryDB<int, Booking>, IRepository<int, Booking>
    {
        public BookingRepository(RoadReadyDbContext context) : base(context) { }

        public override async Task<IEnumerable<Booking>> GetAllAsync()
            => await _context.Bookings.AsNoTracking().ToListAsync();

        public override async Task<Booking?> GetByIdAsync(int key)
            => await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == key);

        public override async Task<Booking?> FindAsync(Expression<Func<Booking, bool>> predicate)
            => await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(predicate);
    }
}

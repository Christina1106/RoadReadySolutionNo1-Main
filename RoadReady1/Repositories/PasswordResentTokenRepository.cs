using Microsoft.EntityFrameworkCore;
using RoadReady1.Context;
using RoadReady1.Interfaces;
using RoadReady1.Models;

namespace RoadReady1.Repositories
{
    public class PasswordResetTokenRepository : RepositoryDB<int, PasswordResetToken>
    {
        public PasswordResetTokenRepository(RoadReadyDbContext ctx) : base(ctx) { }

        public override async Task<IEnumerable<PasswordResetToken>> GetAllAsync()
            => await _context.PasswordResetTokens.AsNoTracking().ToListAsync();

        public override async Task<PasswordResetToken?> GetByIdAsync(int id)
            => await _context.PasswordResetTokens.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

        public override async Task<PasswordResetToken?> FindAsync(System.Linq.Expressions.Expression<Func<PasswordResetToken, bool>> predicate)
            => await _context.PasswordResetTokens.AsNoTracking().FirstOrDefaultAsync(predicate);
    }
}

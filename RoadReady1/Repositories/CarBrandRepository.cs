using Microsoft.EntityFrameworkCore;
using RoadReady1.Context;
using RoadReady1.Interfaces;
using RoadReady1.Models;

namespace RoadReady1.Repositories
{
    public class CarBrandRepository : RepositoryDB<int, CarBrand>
    {
        public CarBrandRepository(RoadReadyDbContext ctx) : base(ctx) { }

        public override async Task<IEnumerable<CarBrand>> GetAllAsync()
            => await _context.CarBrands.AsNoTracking().ToListAsync();

        public override async Task<CarBrand?> GetByIdAsync(int id)
            => await _context.CarBrands.AsNoTracking().FirstOrDefaultAsync(b => b.BrandId == id);

        public override async Task<CarBrand?> FindAsync(System.Linq.Expressions.Expression<Func<CarBrand, bool>> predicate)
            => await _context.CarBrands.AsNoTracking().FirstOrDefaultAsync(predicate);
    }
}

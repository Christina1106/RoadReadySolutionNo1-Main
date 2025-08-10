using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RoadReady1.Context;
using RoadReady1.Interfaces;
using RoadReady1.Models;
using RoadReady1.Exceptions;

namespace RoadReady1.Repositories
{
    /// <summary>
    /// Concrete repository for User entity using generic RepositoryDB.
    /// </summary>
    public class UserRepository : RepositoryDB<int, User>, IRepository<int, User>
    {
        public UserRepository(RoadReadyDbContext context) : base(context) { }


        /// <summary>
        /// Override GetAll to include related Role entity.
        /// </summary>
        public override async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                                 .Include(u => u.Role)
                                 .ToListAsync();
        }

        /// <summary>
        /// Override GetById to include related Role entity.
        /// </summary>
        public override async Task<User> GetByIdAsync(int key)
        {
            var user = await _context.Users
                                     .Include(u => u.Role)
                                     .SingleOrDefaultAsync(u => u.UserId == key);
            if (user == null)
                throw new NotFoundException($"User {key} not found.");
            return user;
        }
    }
}

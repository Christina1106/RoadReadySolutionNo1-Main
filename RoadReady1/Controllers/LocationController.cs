using Microsoft.AspNetCore.Mvc;
using RoadReady1.Models;
using RoadReady1.Interfaces;

namespace RoadReady1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationsController : ControllerBase
    {
        private readonly IRepository<int, Location> _repo;

        public LocationsController(IRepository<int, Location> repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Location>>> GetAll()
        {
            var locations = await _repo.GetAllAsync();
            return Ok(locations);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Location>> GetById(int id)
        {
            var loc = await _repo.GetByIdAsync(id);
            if (loc == null) return NotFound();
            return Ok(loc);
        }
    }
}

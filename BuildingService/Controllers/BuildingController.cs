using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace BuildingService.Controllers
{
    [Route("api/building")]
    [ApiController]
    public class BuildingController : ControllerBase
    {
        private readonly BuildingRepository _repository;

        public BuildingController(BuildingRepository repository)
        {
            _repository = repository;
        }

        [HttpPut("{id}")]
        public Task Upsert(string id, [FromBody]Building item)
        {
            item.Id = id;
            return _repository.Upsert(item);
        }

        [HttpGet("{id}")]
        public Task<Building> Get(string id)
        {
            return _repository.GetByIdAsync(id);
        }

        [HttpGet("byOrganization/{id}")]
        public async Task<Building[]> GetAllByOrganization(string id)
        {
            return (await _repository.GetAllAsync(id)).ToArray();
        }
    }
}

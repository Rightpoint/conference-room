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
        public Task<Building> Get(string buildingId)
        {
            return _repository.GetByIdAsync(buildingId);
        }

        [HttpGet("byOrganization/{id}")]
        public async Task<Building[]> GetAllByOrganization(string organizationId)
        {
            return (await _repository.GetAllAsync(organizationId)).ToArray();
        }
    }
}

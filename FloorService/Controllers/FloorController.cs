using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace FloorService.Controllers
{
    [Route("api/floor")]
    [ApiController]
    public class FloorController : ControllerBase
    {
        private readonly FloorRepository _repository;

        public FloorController(FloorRepository repository)
        {
            _repository = repository;
        }

        [HttpPut("{id}")]
        public Task Upsert(string id, [FromBody]Floor item)
        {
            item.Id = id;
            return _repository.Upsert(item);
        }

        [HttpGet("{id}")]
        public Task<Floor> Get(string id)
        {
            return _repository.GetByIdAsync(id);
        }

        [HttpGet("all")]
        public async Task<Floor[]> GetAll()
        {
            return (await _repository.GetAllAsync()).ToArray();
        }

        [HttpGet("byBuilding/{buildingId}")]
        public async Task<Floor[]> GetAllByBuilding(string buildingId)
        {
            return (await _repository.GetAllByBuildingAsync(buildingId)).ToArray();
        }
    }
}

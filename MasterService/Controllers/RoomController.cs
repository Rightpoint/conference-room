using System.Linq;
using System.Threading.Tasks;
using MasterService.Models;
using MasterService.Repository;
using Microsoft.AspNetCore.Mvc;

namespace MasterService.Controllers
{
    [Route("api/room")]
    [ApiController]
    public class RoomController : ControllerBase
    {
        private readonly RoomRepository _repository;

        public RoomController(RoomRepository repository)
        {
            _repository = repository;
        }

        [HttpPut("{organizationId}/{id}")]
        public Task Upsert(string organizationId, string id, [FromBody]Room item)
        {
            item.OrganizationId = organizationId;
            item.Id = id;
            return _repository.Upsert(item);
        }

        [HttpGet("{organizationId}/{id}")]
        public Task<Room> Get(string organizationId, string id)
        {
            return _repository.GetByIdAsync(organizationId, id);
        }

        [HttpGet("{organizationId}/{buildingId}/all")]
        public async Task<Room[]> GetAllByBuilding(string organizationId, string buildingId)
        {
            return (await _repository.GetAllByBuildingAsync(organizationId, buildingId)).ToArray();
        }

        [HttpGet("{organizationId}")]
        public async Task<Room[]> GetAll(string organizationId)
        {
            return (await _repository.GetAllAsync(organizationId)).ToArray();
        }
    }
}

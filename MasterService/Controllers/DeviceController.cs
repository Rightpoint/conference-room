using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MasterService.Models;
using MasterService.Repository;
using Microsoft.AspNetCore.Mvc;

namespace MasterService.Controllers
{
    [Route("api/device")]
    [ApiController]
    public class DeviceController : ControllerBase
    {
        private readonly DeviceRepository _repository;

        public DeviceController(DeviceRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("{organizationId}/{id}")]
        public Task<Device> Get(string organizationId, string id)
        {
            return _repository.GetByIdAsync(organizationId, id);
        }

        [HttpGet("{organizationId}")]
        public async Task<Device[]> GetAllByOrganization(string organizationId)
        {
            return (await _repository.GetAllAsync(organizationId)).ToArray();
        }

        [HttpPut("{id}")]
        public Task Upsert(string id, [FromBody]Device item)
        {
            item.Id = id;
            return _repository.Upsert(item);
        }
    }
}

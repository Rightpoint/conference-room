using System;
using System.Linq;
using System.Threading.Tasks;
using MasterService.Models;
using MasterService.Repository;
using Microsoft.AspNetCore.Mvc;

namespace MasterService.Controllers
{
    [Route("api/deviceStatus")]
    [ApiController]
    public class DeviceStatusController : ControllerBase
    {
        private readonly DeviceStatusRepository _repository;

        public DeviceStatusController(DeviceStatusRepository repository)
        {
            _repository = repository;
        }

        [HttpPut("{id}")]
        public Task Upsert(string id, [FromBody]DeviceStatus item)
        {
            item.Id = id;
            return _repository.Upsert(item);
        }

        [HttpGet("{organizationId}/{deviceId}/range")]
        public async Task<DeviceStatus[]> Get(string organizationId, string deviceId, DateTimeOffset start, DateTimeOffset end)
        {
            return (await _repository.GetRangeAsync(organizationId, deviceId, start, end)).ToArray();
        }

        [HttpGet("{organizationId}/range")]
        public async Task<DeviceStatus[]> Get(string organizationId, DateTimeOffset start, DateTimeOffset end)
        {
            return (await _repository.GetRangeAsync(organizationId, start, end)).ToArray();
        }
    }
}

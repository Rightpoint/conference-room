using System.Linq;
using System.Threading.Tasks;
using MasterService.Models;
using MasterService.Repository;
using Microsoft.AspNetCore.Mvc;

namespace MasterService.Controllers
{
    [Route("api/meetingExtension")]
    [ApiController]
    public class MeetingExtensionController : ControllerBase
    {
        private readonly MeetingExtensionRepository _repository;

        public MeetingExtensionController(MeetingExtensionRepository repository)
        {
            _repository = repository;
        }

        [HttpPut("{id}")]
        public Task Upsert(string id, [FromBody]MeetingExtension item)
        {
            item.Id = id;
            return _repository.Upsert(item);
        }

        [HttpGet("{organizationId}/{id}")]
        public Task<MeetingExtension> Get(string organizationId, string id)
        {
            return _repository.GetByIdAsync(organizationId, id);
        }

        [HttpGet("{organizationId}")]
        public async Task<MeetingExtension[]> Get(string organizationId, string[] id)
        {
            return (await _repository.GetByIdAsync(organizationId, id)).ToArray();
        }
    }
}

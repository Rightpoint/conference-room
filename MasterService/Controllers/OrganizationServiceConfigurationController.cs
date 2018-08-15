using System.Linq;
using System.Threading.Tasks;
using MasterService.Models;
using MasterService.Repository;
using Microsoft.AspNetCore.Mvc;

namespace MasterService.Controllers
{
    [Route("api/organizationServiceConfiguration")]
    [ApiController]
    public class OrganizationServiceConfigurationController : ControllerBase
    {
        private readonly OrganizationServiceConfigurationRepository _repository;

        public OrganizationServiceConfigurationController(OrganizationServiceConfigurationRepository repository)
        {
            _repository = repository;
        }

        [HttpPut("{organizationId}/{serviceName}")]
        public Task Upsert(string organizationId, string serviceName, [FromBody]OrganizationServiceConfiguration item)
        {
            item.OrganizationId = organizationId;
            item.ServiceName = serviceName;
            return _repository.Upsert(item);
        }

        [HttpGet("{organizationId}/{serviceName}")]
        public Task<OrganizationServiceConfiguration> Get(string organizationId, string serviceName)
        {
            return _repository.GetByIdAsync(organizationId, serviceName);
        }

        [HttpGet("{organizationId}")]
        public async Task<OrganizationServiceConfiguration[]> GetAll(string organizationId)
        {
            return (await _repository.GetAllAsync(organizationId)).ToArray();
        }
    }
}

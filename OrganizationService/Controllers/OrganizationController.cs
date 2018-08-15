using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace OrganizationService.Controllers
{
    [Route("api/organization")]
    [ApiController]
    public class OrganizationController : ControllerBase
    {
        private readonly OrganizationRepository _repository;

        public OrganizationController(OrganizationRepository repository)
        {
            _repository = repository;
        }

        [HttpPut("{id}")]
        public Task Upsert(string id, [FromBody]Organization item)
        {
            item.Id = id;
            return _repository.Upsert(item);
        }

        [HttpGet("{id}")]
        public Task<Organization> Get(string id)
        {
            return _repository.GetByIdAsync(id);
        }

        [HttpGet("all")]
        public async Task<Organization[]> GetAll()
        {
            return (await _repository.GetAllAsync()).ToArray();
        }

        [HttpGet("byUserDomain/{userDomain}")]
        public Task<Organization> GetByUserDomain(string userDomain)
        {
            return _repository.GetByUserDomainAsync(userDomain);
        }

        [HttpGet("byAdministrator/{user}")]
        public async Task<Organization[]> GetByAdministrator(string user)
        {
            return (await _repository.GetByAdministratorAsync(user)).ToArray();
        }
    }
}

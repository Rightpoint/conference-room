using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using RightpointLabs.ConferenceRoom.Domain;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Web.Areas.Admin.Controllers
{
    public class ServiceController : BaseController
    {
        private readonly IOrganizationServiceConfigurationRepository _organizationServiceConfigurationRepository;

        public ServiceController(IOrganizationServiceConfigurationRepository organizationServiceConfigurationRepository, IOrganizationRepository organizationRepository, IGlobalAdministratorRepository globalAdministratorRepository) : base(organizationRepository, globalAdministratorRepository)
        {
            _organizationServiceConfigurationRepository = organizationServiceConfigurationRepository;
        }

        protected override void OnAuthorization(AuthorizationContext filterContext)
        {
            base.OnAuthorization(filterContext);

            if (filterContext.Result == null && null == CurrentOrganization)
            {
                filterContext.Result = RedirectToAction("Index", "Organization");
            }
        }

        public ActionResult Index()
        {
            var services = _organizationServiceConfigurationRepository.GetAll(CurrentOrganization.Id);
            return View(services);
        }

        public ActionResult Create()
        {
            return View("Edit", new OrganizationServiceConfigurationEntity() { OrganizationId = CurrentOrganization.Id });
        }

        [HttpPost]
        public ActionResult Create(string serviceName, string parameters)
        {
            var model = new OrganizationServiceConfigurationEntity()
            {
                OrganizationId = CurrentOrganization.Id,
                ServiceName = serviceName,
                Parameters = JObject.Parse(parameters),
            };
            if (null != _organizationServiceConfigurationRepository.Get(model.OrganizationId, model.ServiceName))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Conflict);
            }

            _organizationServiceConfigurationRepository.Insert(model);
            return RedirectToAction("Index");
        }

        public ActionResult Edit(string id)
        {
            var model = _organizationServiceConfigurationRepository.Get(CurrentOrganization.Id, id);
            if (null == model)
            {
                return HttpNotFound();
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(string serviceName, string parameters)
        {
            var model = new OrganizationServiceConfigurationEntity()
            {
                OrganizationId = CurrentOrganization.Id,
                ServiceName = serviceName,
                Parameters = JObject.Parse(parameters),
            };
            _organizationServiceConfigurationRepository.Update(model);
            return RedirectToAction("Index");
        }

        public ActionResult Details(string id)
        {
            var model = _organizationServiceConfigurationRepository.Get(CurrentOrganization.Id, id);
            if (null == model)
            {
                return HttpNotFound();
            }
            return View(model);
        }
    }
}
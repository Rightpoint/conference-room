using System.Web.Mvc;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;

namespace RightpointLabs.ConferenceRoom.Services.Areas.Admin.Controllers
{
    public class BuildingController : BaseController
    {
        private readonly IBuildingRepository _buildingRepository;

        public BuildingController(IBuildingRepository buildingRepository, IOrganizationRepository organizationRepository, IGlobalAdministratorRepository globalAdministratorRepository) : base(organizationRepository, globalAdministratorRepository)
        {
            _buildingRepository = buildingRepository;
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
            return View(_buildingRepository.GetAll(CurrentOrganization.Id));
        }

        public ActionResult Create()
        {
            return View("Edit");
        }

        [HttpPost]
        public ActionResult Create(BuildingEntity model)
        {
            model.Id = null;
            model.OrganizationId = CurrentOrganization.Id;
            _buildingRepository.Update(model);
            return RedirectToAction("Index");
        }

        public ActionResult Edit(string id)
        {
            var model = _buildingRepository.Get(id);
            if (null == model || model.OrganizationId != CurrentOrganization.Id)
            {
                return HttpNotFound();
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(BuildingEntity model)
        {
            if (_buildingRepository.Get(model.Id)?.OrganizationId != CurrentOrganization.Id)
            {
                return HttpNotFound();
            }

            model.OrganizationId = CurrentOrganization.Id;
            _buildingRepository.Update(model);
            return RedirectToAction("Index");
        }

        public ActionResult Details(string id)
        {
            return Edit(id);
        }
    }
}
using System.Linq;
using System.Web.Mvc;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;

namespace RightpointLabs.ConferenceRoom.Services.Areas.Admin.Controllers
{
    public class FloorController : BaseController
    {
        private readonly IFloorRepository _floorRepository;
        private readonly IBuildingRepository _buildingRepository;

        public FloorController(IFloorRepository floorRepository, IBuildingRepository buildingRepository, IOrganizationRepository organizationRepository, IGlobalAdministratorRepository globalAdministratorRepository) : base(organizationRepository, globalAdministratorRepository)
        {
            _floorRepository = floorRepository;
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
            ViewBag.Buildings = _buildingRepository.GetAll(CurrentOrganization.Id).ToDictionary(_ => _.Id, _ => _.Name);
            return View(_floorRepository.GetAllByOrganization(CurrentOrganization.Id));
        }

        public ActionResult Create()
        {
            return View("Edit");
        }

        [HttpPost]
        public ActionResult Create(FloorEntity model)
        {
            model.Id = null;
            model.OrganizationId = CurrentOrganization.Id;
            _floorRepository.Update(model);
            return RedirectToAction("Index");
        }

        public ActionResult Edit(string id)
        {
            var model = _floorRepository.Get(id);
            if (null == model || model.OrganizationId != CurrentOrganization.Id)
            {
                return HttpNotFound();
            }

            ViewBag.Building = _buildingRepository.Get(model.BuildingId)?.Name;
            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(FloorEntity model)
        {
            if (_floorRepository.Get(model.Id)?.OrganizationId != CurrentOrganization.Id)
            {
                return HttpNotFound();
            }

            model.OrganizationId = CurrentOrganization.Id;
            _floorRepository.Update(model);
            return RedirectToAction("Index");
        }

        public ActionResult Details(string id)
        {
            return Edit(id);
        } 
    }
}
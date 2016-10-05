using System.Linq;
using System.Web.Mvc;
using RightpointLabs.ConferenceRoom.Domain;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;

namespace RightpointLabs.ConferenceRoom.Services.Areas.Admin.Controllers
{
    public class RoomController : BaseController
    {
        private readonly IRoomMetadataRepository _roomRepository;
        private readonly IFloorRepository _floorRepository;
        private readonly IBuildingRepository _buildingRepository;

        public RoomController(IRoomMetadataRepository roomRepository, IFloorRepository floorRepository, IBuildingRepository buildingRepository, IOrganizationRepository organizationRepository, IGlobalAdministratorRepository globalAdministratorRepository) : base(organizationRepository, globalAdministratorRepository)
        {
            _roomRepository = roomRepository;
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
            var buildings = _buildingRepository.GetAll(CurrentOrganization.Id).ToDictionary(_ => _.Id, _ => _.Name);
            ViewBag.Floors = _floorRepository.GetAllByOrganization(CurrentOrganization.Id)
                .ToDictionary(_ => _.Id, _ => string.Format("{0} - {1}", buildings.TryGetValue(_.BuildingId), _.Name));
            return View(_roomRepository.GetRoomInfosForOrganization(CurrentOrganization.Id));
        }

        public ActionResult Create()
        {
            return View("Edit");
        }

        [HttpPost]
        public ActionResult Create(RoomMetadataEntity model)
        {
            var floor = _floorRepository.Get(model.FloorId);
            if (floor.OrganizationId != CurrentOrganization.Id)
            {
                return HttpNotFound();
            }

            model.Id = null;
            model.OrganizationId = CurrentOrganization.Id;
            model.BuildingId = floor.BuildingId;
            _roomRepository.Update(model);
            return RedirectToAction("Index");
        }

        public ActionResult Edit(string id)
        {
            var model = _roomRepository.GetRoomInfo(id);
            if (null == model || model.OrganizationId != CurrentOrganization.Id)
            {
                return HttpNotFound();
            }

            ViewBag.Building = _buildingRepository.Get(model.BuildingId)?.Name;
            ViewBag.Floor = _floorRepository.Get(model.FloorId)?.Name;
            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(RoomMetadataEntity model)
        {
            var room = _roomRepository.GetRoomInfo(model.Id);
            if (null == room || room.OrganizationId != CurrentOrganization.Id)
            {
                return HttpNotFound();
            }

            var floor = _floorRepository.Get(model.FloorId);
            if (floor.OrganizationId != CurrentOrganization.Id)
            {
                return HttpNotFound();
            }

            model.OrganizationId = CurrentOrganization.Id;
            model.BuildingId = floor.BuildingId;

            // values to keep
            model.BeaconUid = room.BeaconUid;
            model.GdoDeviceId = room.GdoDeviceId;
            model.DistanceFromFloorOrigin = room.DistanceFromFloorOrigin;

            _roomRepository.Update(model);
            return RedirectToAction("Index");
        }

        public ActionResult Details(string id)
        {
            return Edit(id);
        } 
    }
}
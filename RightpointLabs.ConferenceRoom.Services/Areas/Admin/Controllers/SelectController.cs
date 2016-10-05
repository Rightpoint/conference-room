using System.Linq;
using System.Net;
using System.Web.Mvc;
using RightpointLabs.ConferenceRoom.Domain;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;

namespace RightpointLabs.ConferenceRoom.Services.Areas.Admin.Controllers
{
    public class SelectController : BaseController
    {
        private readonly IRoomMetadataRepository _roomRepository;
        private readonly IFloorRepository _floorRepository;
        private readonly IBuildingRepository _buildingRepository;

        public SelectController(IRoomMetadataRepository roomRepository, IFloorRepository floorRepository, IBuildingRepository buildingRepository, IOrganizationRepository organizationRepository, IGlobalAdministratorRepository globalAdministratorRepository) : base(organizationRepository, globalAdministratorRepository)
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
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }
        }

        public JsonResult Buildings()
        {
            var buildings = _buildingRepository.GetAll(CurrentOrganization.Id);
            return Json(buildings.Select(_ => new {id = _.Id, text = _.Name}).OrderBy(_ => _.text), JsonRequestBehavior.AllowGet);
        }

        public JsonResult Floors()
        {
            var buildings = _buildingRepository.GetAll(CurrentOrganization.Id).ToDictionary(_ => _.Id, _ => _.Name);
            var floors = _floorRepository.GetAllByOrganization(CurrentOrganization.Id);
            return Json(floors.Select(_ => new { id = _.Id, text = string.Format("{0} - {1}", buildings.TryGetValue(_.BuildingId), _.Name) }).OrderBy(_ => _.text), JsonRequestBehavior.AllowGet);
        }

        public JsonResult Rooms()
        {
            var buildings = _buildingRepository.GetAll(CurrentOrganization.Id).ToDictionary(_ => _.Id, _ => _.Name);
            var floors = _floorRepository.GetAllByOrganization(CurrentOrganization.Id).ToDictionary(_ => _.Id, _ => _.Name);
            var rooms = _roomRepository.GetRoomInfosForOrganization(CurrentOrganization.Id);
            return Json(rooms.Select(_ => new { id = _.RoomAddress, text = string.Format("{0} - {1} - {2}", buildings.TryGetValue(_.BuildingId), floors.TryGetValue(_.FloorId), _.RoomAddress) }).OrderBy(_ => _.text), JsonRequestBehavior.AllowGet);
        }
    }
}
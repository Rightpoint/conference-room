using System.Linq;
using System.Net;
using System.Web.Mvc;
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
   }
}
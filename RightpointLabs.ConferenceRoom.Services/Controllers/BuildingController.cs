using System.Linq;
using log4net;
using RightpointLabs.ConferenceRoom.Domain.Services;
using System.Reflection;
using System.Web.Http;
using RightpointLabs.ConferenceRoom.Domain.Repositories;

namespace RightpointLabs.ConferenceRoom.Services.Controllers
{
    /// <summary>
    /// Operations dealing with room lists
    /// </summary>
    [RoutePrefix("api/building")]
    public class BuildingController : BaseController
    {
        private static readonly ILog __log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IBuildingRepository _buildingRepository;
        private readonly IContextService _contextService;

        public BuildingController(IBuildingRepository buildingRepository, IContextService contextService)
            : base(__log)
        {
            _buildingRepository = buildingRepository;
            _contextService = contextService;
        }

        [Route("all")]
        public object GetAll()
        {
            return _buildingRepository.GetAll(_contextService.CurrentOrganization?.Id).Select(_ => new { _.Id, _.Name });
        }
    }
}

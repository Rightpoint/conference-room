using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using log4net;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Web.Controllers
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
        public async Task<object> GetAll()
        {
            return (await _buildingRepository.GetAllAsync(_contextService.CurrentOrganization?.Id)).Select(_ => new { _.Id, _.Name });
        }
    }
}

using System.Web.Mvc;
using RightpointLabs.ConferenceRoom.Domain.Repositories;

namespace RightpointLabs.ConferenceRoom.Web.Areas.Admin.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(IOrganizationRepository organizationRepository, IGlobalAdministratorRepository globalAdministratorRepository) : base(organizationRepository, globalAdministratorRepository)
        {
        }

        public ActionResult Index()
        {
            return View();
        }
    }
}
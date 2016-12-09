using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web.Mvc;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using AuthorizationContext = System.Web.Mvc.AuthorizationContext;

namespace RightpointLabs.ConferenceRoom.Web.Areas.Admin.Controllers
{
    public abstract class BaseController : Controller
    {
        private static readonly string SelectedOrganizationIdKey = "SelectedOrganizationId";

        private readonly IOrganizationRepository _organizationRepository;
        private readonly IGlobalAdministratorRepository _globalAdministratorRepository;

        protected BaseController(IOrganizationRepository organizationRepository, IGlobalAdministratorRepository globalAdministratorRepository)
        {
            _organizationRepository = organizationRepository;
            _globalAdministratorRepository = globalAdministratorRepository;
        }

        protected OrganizationEntity CurrentOrganization { get; private set; }
        protected Lazy<List<OrganizationEntity>> MyOrganizations { get; private set; }
        protected bool IsGlobalAdmin { get; private set; }

        protected override void OnAuthorization(AuthorizationContext filterContext)
        {
            var cp = ClaimsPrincipal.Current;
            if (null == cp)
            {
                // TODO: figure out how to redirect to trigger the auth
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Unauthorized, "Not authenticated");
                return;
            }

            var username = cp.Identities.FirstOrDefault(_ => _.IsAuthenticated && _.AuthenticationType == "AzureAdAuthCookie")?.Name;
            if (string.IsNullOrEmpty(username))
            {
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Unauthorized, "Cannot find username");
                return;
            }

            if (_globalAdministratorRepository.IsGlobalAdmin(username))
            {
                IsGlobalAdmin = true;
                CurrentOrganization = _organizationRepository.Get(Session[SelectedOrganizationIdKey] as string);
                MyOrganizations = new Lazy<List<OrganizationEntity>>(() => _organizationRepository.GetAll().ToList());
            }
            else
            {
                var orgs = _organizationRepository.GetByAdministrator(username).ToList();
                if (!orgs.Any())
                {
                    _globalAdministratorRepository.EnsureRecordExists(); // make sure there's a record there for the user to find (also makes sure our DB is set up)
                    filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden, $"Not an admin ({username})");
                    return;
                }

                IsGlobalAdmin = false;
                CurrentOrganization = orgs.SingleOrDefault(i => i.Id == Session[SelectedOrganizationIdKey] as string);
                MyOrganizations = new Lazy<List<OrganizationEntity>>(() => orgs);
            }

            base.OnAuthorization(filterContext);
        }

        protected void SetCurrentOrganization(string id)
        {
            Session[SelectedOrganizationIdKey] = id;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;

namespace RightpointLabs.ConferenceRoom.Services.Areas.Admin.Controllers
{
    public class OrganizationController : BaseController
    {
        private readonly IOrganizationRepository _organizationRepository;

        public OrganizationController(IOrganizationRepository organizationRepository, IGlobalAdministratorRepository globalAdministratorRepository) : base(organizationRepository, globalAdministratorRepository)
        {
            _organizationRepository = organizationRepository;
        }

        public ActionResult Index()
        {
            ViewBag.CurrentOrg = CurrentOrganization?.Id;
            return View(MyOrganizations?.Value);
        }

        public ActionResult SetAsCurrent(string id)
        {
            SetCurrentOrganization(id);
            return RedirectToAction("Index");
        }

        public ActionResult Create()
        {
            if (IsGlobalAdmin)
            {
                return View("Edit");
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Global admin rights required");
            }
        }

        [HttpPost]
        public ActionResult Create(OrganizationEntity model)
        {
            if (!IsGlobalAdmin)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Global admin rights required");
            }

            model.Id = null;
            _organizationRepository.Save(model);
            return RedirectToAction("Index");
        }

        public ActionResult Edit(string id)
        {
            var org = IsGlobalAdmin ? _organizationRepository.Get(id) : MyOrganizations.Value.SingleOrDefault(_ => _.Id == id);
            if (null == org)
            {
                return HttpNotFound();
            }
            return View(org);
        }

        [HttpPost]
        public ActionResult Edit(OrganizationEntity model)
        {
            var org = IsGlobalAdmin ? _organizationRepository.Get(model.Id) : MyOrganizations.Value.SingleOrDefault(_ => _.Id == model.Id);
            if (null == org)
            {
                return HttpNotFound();
            }

            org.Administrators = model.Administrators;
            org.UserDomains = model.UserDomains;
            _organizationRepository.Save(org);
            return RedirectToAction("Index");
        }

        public ActionResult EditJoinKey(string id)
        {
            return Edit(id);
        }

        [HttpPost]
        public ActionResult EditJoinKey(OrganizationEntity model)
        {
            var org = IsGlobalAdmin ? _organizationRepository.Get(model.Id) : MyOrganizations.Value.SingleOrDefault(_ => _.Id == model.Id);
            if (null == org)
            {
                return HttpNotFound();
            }

            org.JoinKey = model.JoinKey;
            _organizationRepository.Save(org);
            return RedirectToAction("Index");
        }

        public ActionResult Details(string id)
        {
            var org = IsGlobalAdmin ? _organizationRepository.Get(id) : MyOrganizations.Value.SingleOrDefault(_ => _.Id == id);
            if (null == org)
            {
                return HttpNotFound();
            }
            return View("Details", org);
        }
    }
}
using log4net;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Services;
using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.ServiceModel.Channels;
using System.Threading;
using System.Web;
using System.Web.Http;

namespace RightpointLabs.ConferenceRoom.Services.Controllers
{
    /// <summary>
    /// Building metadata
    /// </summary>
    [RoutePrefix("api/building")]
    public class BuildingController : BaseController
    {
        private static readonly ILog __log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IBuildingService _buildingService;

        public BuildingController(IBuildingService buildingService)
            : base(__log)
        {
            _buildingService = buildingService;
        }

        /// <summary>
        /// Gets the info for a single building.
        /// </summary>
        /// <param name="buildingId">The ID of the building</param>
        /// <returns></returns>
        [Route("{buildingId}")]
        public object GetBuilding(string buildingId)
        {
            var data = _buildingService.Get(buildingId);
            return data;
        }

        /// <summary>
        /// Adds the info for a single building.
        /// </summary>
        /// <param name="buildingInfo">The metadata associated with the building.</param>
        /// <returns></returns>
        public void PostBuilding(PostBuildingInfo buildingInfo)
        {
            VerifySecurityCode(buildingInfo.Code);
            _buildingService.Add(buildingInfo);
        }

        /// <summary>
        /// Updates the info for a single building.
        /// </summary>
        /// <param name="buildingId">The ID of the building</param>
        /// <param name="buildingInfo">The metadata associated with the building.</param>
        /// <returns></returns>
        [Route("{buildingId}")]
        public void PutBuilding(string buildingId, PostBuildingInfo buildingInfo)
        {
            VerifySecurityCode(buildingInfo.Code);
            _buildingService.Update(buildingId, buildingInfo);
        }

        /// <summary>
        /// Verify that the correct security code has been provided to the API.
        /// </summary>
        /// <param name="securityCode"></param>
        private void VerifySecurityCode(string securityCode)
        {
            var realCode = ConfigurationManager.AppSettings["settingsSecurityCode"];
            if (securityCode != realCode)
            {
                Thread.Sleep(1000);
                throw new AccessDeniedException("Access denied", null);
            }
        }
    }
}

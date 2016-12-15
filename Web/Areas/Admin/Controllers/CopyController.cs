using System.Web.Mvc;
using RightpointLabs.ConferenceRoom.Domain.Repositories;

using Mongo = RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories.Mongo;
using AzureTable = RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories.AzureTable;

namespace RightpointLabs.ConferenceRoom.Web.Areas.Admin.Controllers
{
    public class CopyController : Controller
    {
        private readonly Mongo.BuildingRepository _buildingRepository;
        private readonly Mongo.DeviceRepository _deviceRepository;
        private readonly Mongo.FloorRepository _floorRepository;
        private readonly Mongo.OrganizationRepository _organizationRepository;
        private readonly Mongo.OrganizationServiceConfigurationRepository _organizationServiceConfigurationRepository;
        private readonly Mongo.RoomMetadataRepository _roomMetadataRepository;
        private readonly AzureTable.BuildingRepository _abuildingRepository;
        private readonly AzureTable.DeviceRepository _adeviceRepository;
        private readonly AzureTable.FloorRepository _afloorRepository;
        private readonly AzureTable.OrganizationRepository _aorganizationRepository;
        private readonly AzureTable.OrganizationServiceConfigurationRepository _aorganizationServiceConfigurationRepository;
        private readonly AzureTable.RoomMetadataRepository _aroomMetadataRepository;

        public CopyController(
            Mongo.BuildingRepository buildingRepository,
            Mongo.DeviceRepository deviceRepository,
            Mongo.FloorRepository floorRepository,
            Mongo.OrganizationRepository organizationRepository,
            Mongo.OrganizationServiceConfigurationRepository organizationServiceConfigurationRepository,
            Mongo.RoomMetadataRepository roomMetadataRepository,
            AzureTable.BuildingRepository AbuildingRepository,
            AzureTable.DeviceRepository AdeviceRepository,
            AzureTable.FloorRepository AfloorRepository,
            AzureTable.OrganizationRepository AorganizationRepository,
            AzureTable.OrganizationServiceConfigurationRepository AorganizationServiceConfigurationRepository,
            AzureTable.RoomMetadataRepository AroomMetadataRepository
            )
        {
            _buildingRepository = buildingRepository;
            _deviceRepository = deviceRepository;
            _floorRepository = floorRepository;
            _organizationRepository = organizationRepository;
            _organizationServiceConfigurationRepository = organizationServiceConfigurationRepository;
            _roomMetadataRepository = roomMetadataRepository;
            _abuildingRepository = AbuildingRepository;
            _adeviceRepository = AdeviceRepository;
            _afloorRepository = AfloorRepository;
            _aorganizationRepository = AorganizationRepository;
            _aorganizationServiceConfigurationRepository = AorganizationServiceConfigurationRepository;
            _aroomMetadataRepository = AroomMetadataRepository;
        }

        public ActionResult Index()
        {
            foreach (var b in _buildingRepository.GetAll()) { _abuildingRepository.Upsert(b); }
            foreach (var b in _deviceRepository.GetAll()) { _adeviceRepository.Upsert(b); }
            foreach (var b in _floorRepository.GetAll()) { _afloorRepository.Upsert(b); }
            foreach (var b in _organizationRepository.GetAll()) { _aorganizationRepository.Upsert(b); }
            foreach (var b in _organizationServiceConfigurationRepository.GetAll()) { _aorganizationServiceConfigurationRepository.Upsert(b); }
            foreach (var b in _roomMetadataRepository.GetAll()) { _aroomMetadataRepository.Upsert(b); }

            return Content("Done");
        }
    }
}
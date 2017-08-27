using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest
{
    public class MeetingCacheReloaderFactory
    {
        private readonly IIOCContainer _rootContainer;

        public MeetingCacheReloaderFactory(IIOCContainer rootContainer)
        {
            _rootContainer = rootContainer;
        }

        public IMeetingCacheReloader CreateForOrganization(OrganizationEntity org)
        {
            return new MeetingCacheReloader(_rootContainer, org);
        }

        private class MeetingCacheReloader : IMeetingCacheReloader
        {
            private readonly IIOCContainer _rootContainer;
            private readonly OrganizationEntity _org;

            public MeetingCacheReloader(IIOCContainer rootContainer, OrganizationEntity org)
            {
                _rootContainer = rootContainer;
                _org = org;
            }

            public Task ReloadCache(string roomAddress)
            {
                using (var c = _rootContainer.CreateChildContainer())
                {
                    c.RegisterInstance((IContextService)new OrganizationContextService(_org));
                    return c.Resolve<IConferenceRoomService>().GetStatus(new RoomMetadataEntity() { RoomAddress = roomAddress, OrganizationId = _org.Id });
                }
            }

            private class OrganizationContextService : IContextService
            {
                private readonly OrganizationEntity _org;

                public OrganizationContextService(OrganizationEntity org)
                {
                    _org = org;
                }

                public bool IsAuthenticated => true;
                public string UserId => null;
                public DeviceEntity CurrentDevice => null;
                public OrganizationEntity CurrentOrganization => _org;
            }
        }
    }
}

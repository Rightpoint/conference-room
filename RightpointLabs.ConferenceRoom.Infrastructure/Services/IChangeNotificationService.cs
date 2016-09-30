using System;
using Microsoft.Exchange.WebServices.Data;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public interface IChangeNotificationService
    {
        void TrackRoom(string roomAddress, IExchangeServiceManager exchangeServiceManager, OrganizationEntity organization);
        bool IsTrackedForChanges(string roomAddress);
    }
}
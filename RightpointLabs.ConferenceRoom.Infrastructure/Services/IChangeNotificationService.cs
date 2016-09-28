using System;
using Microsoft.Exchange.WebServices.Data;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public interface IChangeNotificationService
    {
        void TrackRoom(string roomAddress, IExchangeServiceManager exchangeServiceBuilder);
        bool IsTrackedForChanges(string roomAddress);
    }
}
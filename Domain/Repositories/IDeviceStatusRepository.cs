using System;
using System.Collections.Generic;
using RightpointLabs.ConferenceRoom.Domain.Models;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface IDeviceStatusRepository
    {
        void Insert(DeviceStatus status);
        IEnumerable<DeviceStatus> GetRange(string organizationId, DateTime start, DateTime end);
        IEnumerable<DeviceStatus> GetRange(string organizationId, string deviceId, DateTime start, DateTime end);
    }
}
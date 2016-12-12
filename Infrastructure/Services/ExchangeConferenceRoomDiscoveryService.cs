using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using log4net;
using Microsoft.Exchange.WebServices.Data;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Services;
using Task = System.Threading.Tasks.Task;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class ExchangeConferenceRoomDiscoveryService : IConferenceRoomDiscoveryService
    {
        private readonly ISimpleTimedCache _simpleTimedCache;
        private readonly IExchangeServiceManager _exchangeServiceManager;
        private static readonly ILog __log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ExchangeConferenceRoomDiscoveryService(ISimpleTimedCache simpleTimedCache, IExchangeServiceManager exchangeServiceManager)
        {
            _simpleTimedCache = simpleTimedCache;
            _exchangeServiceManager = exchangeServiceManager;
        }

        /// <summary>
        /// Get all room lists defined on the Exchange server.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RoomList> GetRoomLists()
        {
            return _simpleTimedCache.GetCachedValue("RoomLists", TimeSpan.FromHours(24),
                () => Task.FromResult(_exchangeServiceManager.Execute(string.Empty, svc => svc.GetRoomLists()
                    .Select(i => new RoomList {Address = i.Address, Name = i.Name})
                    .ToArray()))).Result;
        }

        /// <summary>
        /// Gets all the rooms in the specified room list.
        /// </summary>
        /// <param name="roomListAddress">The room list address returned from <see cref="GetRoomLists"/></param>
        /// <returns></returns>
        public IEnumerable<Room> GetRoomsFromRoomList(string roomListAddress)
        {
            return _simpleTimedCache.GetCachedValue("Rooms_" + roomListAddress,
                TimeSpan.FromHours(24),
                () => Task.FromResult(_exchangeServiceManager.Execute(string.Empty, svc => svc.GetRooms(roomListAddress)
                    .Select(i => new Room {Address = i.Address, Name = i.Name})
                    .ToArray()))).Result;
        }

        public Task<string> GetRoomName(string roomAddress)
        {
            try
            {
                return _simpleTimedCache.GetCachedValue("RoomInfo_" + roomAddress,
                    TimeSpan.FromHours(24),
                    () => Task.FromResult(_exchangeServiceManager.Execute(roomAddress, svc => svc.ResolveName(roomAddress).SingleOrDefault()?.Mailbox?.Name)));
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot get name of {roomAddress}", ex);
            }
        }
    }
}

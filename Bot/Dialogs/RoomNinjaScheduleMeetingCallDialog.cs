using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using RightpointLabs.ConferenceRoom.Bot.Services;

namespace RightpointLabs.ConferenceRoom.Bot.Dialogs
{
    [Serializable]
    public class RoomNinjaScheduleMeetingCallDialog : RoomNinjaCallDialogBase<string>
    {
        private readonly string _roomId;
        private readonly DateTimeOffset _startTime;
        private readonly DateTimeOffset _endTime;

        public RoomNinjaScheduleMeetingCallDialog(Uri requestUri, string roomId, DateTimeOffset startTime, DateTimeOffset endTime) : base(requestUri)
        {
            _roomId = roomId;
            _startTime = startTime;
            _endTime = endTime;
        }

        protected override async Task<string> DoWork(IDialogContext context, RoomsService api)
        {
            return await api.ScheduleMeeting(_roomId, _startTime, _endTime);
        }
    }
}

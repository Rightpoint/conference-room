namespace RightpointLabs.ConferenceRoom.Domain.Models
{
    public class RoomStatusInfo
    {
        public bool IsTrackingChanges { get; set; }
        public RoomStatus Status { get; set; }
        public double? NextChangeSeconds { get; set; }
        public Meeting CurrentMeeting { get; set; }
        public Meeting NextMeeting { get; set; }
        public Meeting[] NearTermMeetings { get; set; }
    }
}
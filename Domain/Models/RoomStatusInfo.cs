namespace RightpointLabs.ConferenceRoom.Domain.Models
{
    public class RoomStatusInfo
    {
        public bool IsTrackingChanges { get; set; }
        public RoomStatus Status { get; set; }
        public double? NextChangeSeconds { get; set; }
        public double? RoomNextFreeInSeconds { get; set; }
        public Meeting CurrentMeeting { get; set; }
        public Meeting NextMeeting { get; set; }
        public Meeting PreviousMeeting { get; set; }
        public Meeting[] NearTermMeetings { get; set; }
        public int? warnDelay { get; set; }
        public int? cancelDelay { get; set; }
    }
}
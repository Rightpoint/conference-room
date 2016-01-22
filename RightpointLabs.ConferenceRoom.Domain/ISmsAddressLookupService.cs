namespace RightpointLabs.ConferenceRoom.Domain
{
    public interface ISmsAddressLookupService
    {
        string[] LookupAddresses(string[] emailAddresses);
    }
}
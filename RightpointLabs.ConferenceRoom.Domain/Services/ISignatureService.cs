namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface ISignatureService
    {
        string GetSignature(string input);
        bool VerifySignature(string input, string signature);
    }
}
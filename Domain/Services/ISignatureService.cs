using RightpointLabs.ConferenceRoom.Domain.Models;

namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface ISignatureService
    {
        string GetSignature(IRoom room, string input);
        bool VerifySignature(IRoom room, string input, string signature);
    }
}
using System;
using System.Security.Cryptography;
using System.Text;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class SignatureService : ISignatureService
    {
        private readonly RSACryptoServiceProvider _crypto;

        public SignatureService()
        {
            // make up a new key on each startup - no need for our key to be long-lived
            _crypto = new RSACryptoServiceProvider();
        }

        public string GetSignature(IRoom room, string input)
        {
            var dataBytes = Encoding.UTF8.GetBytes(BuildInput(room, input));
            return Convert.ToBase64String(_crypto.SignData(dataBytes, new SHA256Managed()));
        }

        public bool VerifySignature(IRoom room, string input, string signature)
        {
            var dataBytes = Encoding.UTF8.GetBytes(BuildInput(room, input));
            var sigBytes = Convert.FromBase64String(signature);
            return _crypto.VerifyData(dataBytes, new SHA256Managed(), sigBytes);
        }

        private string BuildInput(IRoom room, string input)
        {
            return $"{room.Id}_{input}";
        }
    }
}
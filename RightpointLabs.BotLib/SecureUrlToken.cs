// Decompiled with JetBrains decompiler
// Type: Microsoft.Bot.Builder.Dialogs.UrlToken
// Assembly: Microsoft.Bot.Builder, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: F35D5002-CC6D-4885-9C04-3DBA3BD9E48C
// Assembly location: C:\projects\Kaizen\Bot\Kaizen.Bot\Bin\Microsoft.Bot.Builder.dll

using System;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace RightpointLabs.BotLib
{
    /// <summary>
    /// Allow object instances to serialized to URLs.  Base64 can not be stored in URLs due to special characters.
    /// </summary>
    /// <remarks>
    /// We use Bson and Gzip to make it small enough to fit within the maximum character limit of URLs.
    /// http://stackoverflow.com/a/32999062 suggests HttpServerUtility's UrlTokenEncode and UrlTokenDecode
    /// is not standards-compliant, but they seem to do the job.
    /// </remarks>
    public static class SecureUrlToken
    {
        /// <summary>Encode an item to be stored in a url.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="item">The item instance.</param>
        /// <returns>The encoded token.</returns>
        public static string Encode<T>(T item)
        {
            var rsa = new RSACryptoServiceProvider();
            var key = ConfigurationManager.AppSettings["EncryptionKey"];
            if (string.IsNullOrEmpty(key))
                throw new Exception("AppSetting 'EncryptionKey' is missing");
            rsa.ImportCspBlob(Convert.FromBase64String(key));

            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                {
                    using (var bsonWriter = new BsonWriter(gzipStream))
                        JsonSerializer.CreateDefault().Serialize(bsonWriter, item);
                }
                return HttpServerUtility.UrlTokenEncode(rsa.Encrypt(memoryStream.ToArray(), true));
            }
        }

        /// <summary>Decode an item from a url token.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="token">The item token.</param>
        /// <returns>The item instance.</returns>
        public static T Decode<T>(string token)
        {
            var rsa = new RSACryptoServiceProvider();
            var key = ConfigurationManager.AppSettings["EncryptionKey"];
            if (string.IsNullOrEmpty(key))
                throw new Exception("AppSetting 'EncryptionKey' is missing");
            rsa.ImportCspBlob(Convert.FromBase64String(key));
            var data = HttpServerUtility.UrlTokenDecode(token);
            data = rsa.Decrypt(data, true);

            using (var memoryStream = new MemoryStream(data))
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    using (var bsonReader = new BsonReader(gzipStream))
                        return JsonSerializer.CreateDefault().Deserialize<T>(bsonReader);
                }
            }
        }
    }
}

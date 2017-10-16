using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using AuthenticationContext = Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext;

namespace RightpointLabs.BotLib
{
    public class AuthController : ApiController
    {
        public class AuthorizeArgs
        {
            public string state { get; set; }
            public string code { get; set; }
            public string error { get; set; }
            public string error_description { get; set; }
            public string session_state { get; set; }

        }

        [Route("api/Auth/Authorize")]
        public async Task<HttpResponseMessage> PostAuthorize([FromBody]AuthorizeArgs a)
        {
            var cookie = SecureUrlToken.Decode<ResumptionCookie>(a.state);
            if (!string.IsNullOrEmpty(a.error))
            {
                await Conversation.ResumeAsync(cookie, new AuthenticationResultModel(cookie.GetMessage()) { Error = a.error, ErrorDescription = a.error_description });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("<html><head><script type='text/javascript'>window.close();</script></head><body>An error occurred during authentication.  You can close this browser window</body></html>", Encoding.UTF8, "text/html")
                };
            }

            // Get access token
            var authContext = new AuthenticationContext(ConfigurationManager.AppSettings["Authority"]);
            var authResult = await authContext.AcquireTokenByAuthorizationCodeAsync(
                a.code,
                new Uri(this.Request.RequestUri.GetLeftPart(UriPartial.Path)),
                new ClientCredential(
                    ConfigurationManager.AppSettings["ClientId"],
                    ConfigurationManager.AppSettings["ClientSecret"]));

            var upn = authResult?.UserInfo?.DisplayableId;

            var result = new AuthenticationResultModel(cookie.GetMessage())
            {
                AccessToken = authResult.IdToken
            };

            if (upn == cookie.GetMessage().From.Id)
            {
                await Conversation.ResumeAsync(cookie, result);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("<html><head><script type='text/javascript'>window.close();</script></head><body>You can close this browser window</body></html>", Encoding.UTF8, "text/html")
                };
            }
            else
            {
                var rnd = new Random();
                result.SecurityKey = string.Join("", Enumerable.Range(0, 6).Select(i => rnd.Next(10).ToString()));
                await Conversation.ResumeAsync(cookie, result);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"<html><head></head><body><!--We can't auto-auth you because {upn} != {cookie.GetMessage().From.Id}. -->Please copy and paste this key into the conversation with the bot: {result.SecurityKey}.</body></html>", Encoding.UTF8, "text/html")
                };
            }
        }
    }
}

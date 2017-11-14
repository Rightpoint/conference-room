using System.Web.Http;
using WebActivatorEx;
using RightpointLabs.ConferenceRoom.Web;
using Swashbuckle.Application;

[assembly: PreApplicationStartMethod(typeof(SwaggerConfig), "Register")]

namespace RightpointLabs.ConferenceRoom.Web
{
    public class SwaggerConfig
    {
        public static void Register()
        {
            var thisAssembly = typeof(SwaggerConfig).Assembly;

            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                    {
                        c.SingleApiVersion("v1", "RightpointLabs.ConferenceRoom.Web");
                    })
                .EnableSwaggerUi(c => { });
        }
    }
}

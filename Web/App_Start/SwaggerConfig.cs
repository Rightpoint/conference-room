using System.Collections.Generic;
using System.Web.Http;
using WebActivatorEx;
using RightpointLabs.ConferenceRoom.Web;
using Swashbuckle.Application;
using Swashbuckle.Swagger;
using System.Web.Http.Description;

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
                        c.SingleApiVersion("v2", "RightpointLabs.ConferenceRoom.Web");
                        c.ApiKey("api_key")
                            .Description("Bearer token here")
                            .Name("Authorization")
                            .In("header");
                        c.OperationFilter<AssignSecurityRequirements>();
                    })
                .EnableSwaggerUi(c =>
                {
                    c.DocumentTitle("Room Ninja API");
                    c.EnableApiKeySupport("Authorization", "header");
                });
        }

        public class AssignSecurityRequirements : IOperationFilter
        {
            public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
            {
                operation.security = new List<IDictionary<string, IEnumerable<string>>>()
                {
                    new Dictionary<string, IEnumerable<string>>()
                    {
                        {
                            "api_key", new string[0]
                        }
                    }
                };
            }
        }
    }
}

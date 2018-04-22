using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RightpointLabs.ConferenceRoom.Bot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();

            Trace.WriteLine("Startup complete");

            var prov = (IActionDescriptorCollectionProvider)app.ApplicationServices.GetRequiredService(typeof(IActionDescriptorCollectionProvider));
            Trace.WriteLine($"Count: {prov.ActionDescriptors.Items.Count}");
            foreach (var x in prov.ActionDescriptors.Items.Select(i =>
                string.Join(", ", i.RouteValues.Select(ii => $"{ii.Key}: {ii.Value}"))))
            {
                Trace.WriteLine(x);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MasterService.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;

namespace MasterService
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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddSingleton(sp => CloudStorageAccount.Parse(Configuration.GetConnectionString("AzureStorage")).CreateCloudTableClient());
            services.AddTransient<DeviceRepository>();
            services.AddTransient<DeviceStatusRepository>();
            services.AddTransient<MeetingExtensionRepository>();
            services.AddTransient<OrganizationServiceConfigurationRepository>();
            services.AddTransient<RoomRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();

            using (var scope = app.ApplicationServices.CreateScope())
            {
                Task.WaitAll(new[]
                {
                    scope.ServiceProvider.GetService<DeviceRepository>().InitAsync(),
                    scope.ServiceProvider.GetService<DeviceStatusRepository>().InitAsync(),
                    scope.ServiceProvider.GetService<MeetingExtensionRepository>().InitAsync(),
                    scope.ServiceProvider.GetService<OrganizationServiceConfigurationRepository>().InitAsync(),
                    scope.ServiceProvider.GetService<RoomRepository>().InitAsync(),
                });
            }
        }
    }
}

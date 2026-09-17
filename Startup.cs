using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.DependencyInjection;
using EPiServer.Scheduler;
using EPiServer.Web.Routing;
using Optimizely26.Business.Extensions;

namespace Optimizely26
{
    public class Startup(IWebHostEnvironment webHostingEnvironment)
    {
        public void ConfigureServices(IServiceCollection services)
        {
            if (webHostingEnvironment.IsDevelopment())
            {
                AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(webHostingEnvironment.ContentRootPath, "App_Data"));

                services.Configure<SchedulerOptions>(options => options.Enabled = false);
            }

            services
                .AddCmsAspNetIdentity<ApplicationUser>()
                .AddCms()
                .AddNackademin()
                .AddAdminUserRegistration()
                .AddEmbeddedLocalization<Startup>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapContent();
            });
        }
    }
}

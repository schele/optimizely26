using EPiServer.Cms.Shell.UI.Configurations;
using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.DependencyInjection;
using EPiServer.Scheduler;
using EPiServer.Web.Routing;
using Optimizely26.Business.Extensions;
using Optimizely26.Services;

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
                .AddEmbeddedLocalization<Startup>()


                .Configure<MediaFileOptions>(x => { x.FileSizeLimit = 52428800; });
			
            
            services.AddScoped<IXmlSitemapService, XmlSitemapService>();
            services.AddHttpClient<IOmdbService, OmdbService>();
            services.AddScoped<IMovieRatingService, MovieRatingService>();

            services.AddServerSideBlazor();


		}

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

			app.UseStatusCodePages(context =>
			{
				var httpContext = context.HttpContext;

				if (httpContext.Response.StatusCode == 404 &&
					!httpContext.Request.Path.StartsWithSegments("/error"))
				{
					httpContext.Response.Redirect("/error");
				}

				return Task.CompletedTask;
			});

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapContent();
                endpoints.MapBlazorHub();
            });
		}
    }
}

using ITKostnad.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ITKostnad.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Threading;
using Microsoft.AspNetCore.Server.IIS;

namespace ITKostnad
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
            services.AddControllersWithViews();
            services.Configure<AppSettingsModel>(Configuration.GetSection("AppSettings"));
            services.AddMemoryCache();

            services.Configure<IISServerOptions>(options =>
            {
                options.AutomaticAuthentication = true;
            });

            services.AddAuthentication(IISServerDefaults.AuthenticationScheme);
            services.AddAuthorization(options =>
            {
                options.AddPolicy("ITK_Group", policy => policy.RequireRole(Configuration["SecuritySettings:ITK_Group"]));
            });


        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IMemoryCache cache, IOptions<AppSettingsModel> settings)
        {

            ADHelper.Domain = settings.Value.Domain;
            ADHelper.ServiceAccount = settings.Value.ServiceAccount;
            ADHelper.ServiceAccountPassword = settings.Value.ServiceAccountPassword;
            ADHelper.UserOU = settings.Value.UserOU;
            ADHelper.ComputerOU = settings.Value.ComputerOU;
            ADHelper.Cache = cache;

            ADHelper.start();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

    }
}

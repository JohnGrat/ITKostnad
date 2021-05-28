using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITKostnad.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ITKostnad.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Threading;

namespace ITKostnad
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        List<ComputerModel> ADComputers;
        List<UserModel> ADUsers;
        public IConfiguration Configuration { get; }
        public Thread ServiceThread { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.Configure<AppSettingsModel>(Configuration.GetSection("AppSettings"));
            services.AddMemoryCache();

            services.Configure<IISServerOptions>(options => {
                options.AutomaticAuthentication = true;
            });

            services.AddAuthentication(IISServerDefaults.AuthenticationScheme);
            services.AddAuthorization(options => {
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

            ADComputers = ADHelper.GetallComputers();
            ADUsers = ADHelper.GetallAdUsers();


            var entryOptions = new MemoryCacheEntryOptions();
            cache.Set("Computers", ADComputers, entryOptions);
            cache.Set("Users", ADUsers, entryOptions);


            ServiceThread = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(TimeSpan.FromHours(12));
                    try
                    {
                        ADComputers = ADHelper.GetallComputers();
                        ADUsers = ADHelper.GetallAdUsers();
                        cache.Set("Computers", ADComputers, entryOptions);
                        cache.Set("Users", ADUsers, entryOptions);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }


                }
            });
            ServiceThread.Start();

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

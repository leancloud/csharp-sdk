using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LeanCloud;
using LeanCloud.Engine;
using web.LeanDB;

namespace web {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddLogging(builder => {
                builder
                    .AddFilter("Microsoft", LogLevel.Error)
                    .AddFilter("System", LogLevel.Error)
                    .AddConsole();
            });

            LCLogger.LogDelegate = (level, log) => {
                switch (level) {
                    //case LCLogLevel.Debug:
                    //    Console.WriteLine($"[DEBUG] {log}");
                    //    break;
                    case LCLogLevel.Warn:
                        Console.Out.WriteLine($"[WARN] {log}");
                        break;
                    case LCLogLevel.Error:
                        Console.Error.WriteLine($"[ERROR] {log}");
                        break;
                    default:
                        break;
                }
            };
            LCEngine.Initialize(services);

            RedisHelper.Init();
            MySQLHelper.Init();
            MongoHelper.Init();

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}

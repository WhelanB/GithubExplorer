using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Hangfire;
using Octokit;
using System.Net.Http;
using Newtonsoft.Json;

namespace GitHub
{
    public class Startup
    {
        private IHostingEnvironment _env;
        public Startup(IHostingEnvironment env)
        {
            _env = env;
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            //use session for storing access token
            services.AddSession();
            //Use HangFire
            //services.AddHangfire(config =>
              // config.UseSqlServerStorage("Data Source =.\\SQLEXPRESS; Initial Catalog = hangfireJobs; Integrated Security = True"));

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            //Setup HangFire dahboard and Server (located at /hangfire)
            //app.UseHangfireDashboard();
            //app.UseHangfireServer();
            //RecurringJob.AddOrUpdate(() => Process(), Cron.Minutely);
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseSession();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
        /**
        public async Task<string> QueryTrends()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Gitterbug");
                var response =  await client.GetAsync("https://api.github.com/search/repositories?q=stars:>1&sort=stars&order=desc");

                return await response.Content.ReadAsStringAsync();
            }
          
        }

        public TResult RunSync<TResult>(Func<Task<TResult>> func)
        {
            return Task.Run<Task<TResult>>(func).Unwrap().GetAwaiter().GetResult();
        }

        private string Trend()
        {
            return RunSync(() => QueryTrends());
        }

        public void Process()
        {
            var webRoot = _env.WebRootPath;
            var file = System.IO.Path.Combine(webRoot, "Home/data/trends.json");
            string response = Trend();
            Dictionary<string, string> transform = new Dictionary<string, string>();
            //parsed = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            //transform["items"] = parsed["items"];
            System.IO.File.WriteAllText(file, response);
        }**/
    }
}

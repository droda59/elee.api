using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using E133.Api.Infrastructure;
using E133.Database;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace E133.Api
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services
                .AddOptions()
                .AddCors(options => {
                    var policy = new Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicy();
                    policy.Origins.Add("*");
                    policy.Headers.Add("*");
                    policy.Methods.Add("*");
                    policy.SupportsCredentials = true;

                    options.AddPolicy("CorsPolicy", policy);
                })
                .AddMvc()
                .AddJsonOptions(options =>
                {
                    var settings = options.SerializerSettings;
                    
                    settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    settings.NullValueHandling = NullValueHandling.Ignore;
                    settings.Formatting = Formatting.Indented;
                });


            services.AddAuthorization(options => 
            {
                options.AddPolicy("LocalOnly", policy => policy.Requirements.Add(new LocalAuthorizationOnlyRequirement()));
                options.AddPolicy("Admin", policy => policy.Requirements.Add(new GroupAuthorizationRequirement("Admin")));
            });
            
            services.Configure<MongoDBOptions>(Configuration.GetSection("mongodb"));
            MongoDBConfig.RegisterClassMaps();

            return IocConfig.RegisterComponents(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseCors("CorsPolicy");
            app.UseMvc();
        }
    }
}

using Autofac;

using Microsoft.Extensions.Options;
using Microsoft.Framework.Configuration;

using E133.Database;
using E133.Parser.LanguageUtilities;

namespace E133.Crawler
{
    public class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var configSection = configuration.GetSection("mongodb");
            var mongoOptions = new MongoDBOptions
            {
                ConnectionString = configSection["ConnectionString"], 
                DatabaseName = configSection["DatabaseName"]
            };
            var options = Options.Create<MongoDBOptions>(mongoOptions);

            builder.RegisterInstance<IOptions<MongoDBOptions>>(options);

            builder.RegisterType<RicardoCrawler>()
                .As<IHtmlCrawler>()
                .SingleInstance();

            builder.RegisterType<FileVerbProvider>()
                .As<IVerbProvider>()
                .SingleInstance();
        }
    }
}
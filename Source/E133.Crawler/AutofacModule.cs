using Autofac;

using E133.Parser.LanguageUtilities;

namespace E133.Crawler
{
    public class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<RicardoCrawler>()
                .As<IHtmlCrawler>()
                .SingleInstance();

            builder.RegisterType<FileVerbProvider>()
                .As<IVerbProvider>()
                .SingleInstance();
        }
    }
}
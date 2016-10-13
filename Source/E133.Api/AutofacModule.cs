using Autofac;

using E133.Parser.LanguageUtilities;

namespace E133.Api
{
    public class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AspVerbProvider>()
                .As<IVerbProvider>()
                .SingleInstance();
        }
    }
}
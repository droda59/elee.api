using System;

using Microsoft.Extensions.DependencyInjection;

using Autofac;
using Autofac.Extensions.DependencyInjection;

namespace E133.Api
{
    public static class IocConfig
    {
        public static IServiceProvider RegisterComponents(IServiceCollection services)
        {
			var builder = new ContainerBuilder();

            builder.RegisterModule(new E133.Api.AutofacModule());
            builder.RegisterModule(new E133.Business.AutofacModule());
            builder.RegisterModule(new E133.Database.AutofacModule());
            builder.RegisterModule(new E133.Parser.AutofacModule());

            builder.Populate(services);
            
            var container = builder.Build();

            return container.Resolve<IServiceProvider>();
        }
    }
}
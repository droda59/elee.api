using E133.Business;
using E133.Database.Repositories;

using Autofac;

namespace E133.Database
{
    public class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MongoRepository>().As<IQuickRecipeRepository>().SingleInstance();
        }
    }
}

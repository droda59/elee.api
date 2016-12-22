using Autofac;

namespace E133.Business
{
    public class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<HtmlLoader>().As<IHtmlLoader>().SingleInstance();
            builder.RegisterType<RecipeNameGenerator>().As<IRecipeNameGenerator>().SingleInstance();
            builder.RegisterType<NameUnicityOverseer>().As<INameUnicityOverseer>().SingleInstance();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;

using E133.Business.Bases;
using E133.Parser.LanguageUtilities;

using Autofac;

namespace E133.Parser
{
    public class AutofacModule : Module
    {
        private static readonly IEnumerable<CultureInfo> _supportedCultures = new List<CultureInfo>
        {
            new CultureInfo("fr"),
            new CultureInfo("en")
        };

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ParserFactory>().As<IParserFactory>().SingleInstance();

            RegisterDetector<IActionDetector>(builder);
            RegisterDetector<ITimerDetector>(builder);
            RegisterDetector<IIngredientDetector>(builder);
            RegisterDetector<IMeasureUnitDetector>(builder);
            RegisterDetector<ILanguageHelper>(builder);
            RegisterDetector<ISubrecipeRepository>(builder);

            builder.RegisterType<RicardoParser>()
                .As<IHtmlParser>()
                .SingleInstance();
        }

        private static void RegisterDetector<TDetector>(ContainerBuilder builder)
        {
            var typeFrom = typeof(TDetector);
            var baseName = typeFrom.Name.Remove(0, 1);
            var @namespace = typeFrom.Namespace;
            
            foreach (var supportedCulture in _supportedCultures)
            {
                var type = Type.GetType(@namespace + "." + supportedCulture.EnglishName + "." + supportedCulture.EnglishName + baseName);
                builder.RegisterType(type)
                    .Keyed<TDetector>(supportedCulture.EnglishName + baseName)
                    .SingleInstance();
            }

            builder.Register<Func<CultureInfo, TDetector>>(FactoryResolver.ResolveDetectorByLanguage<TDetector>)
                .SingleInstance();
        }
    }
}

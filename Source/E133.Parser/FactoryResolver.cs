using System;
using System.Globalization;

using Autofac;

namespace E133.Parser
{
    internal static class FactoryResolver
    {
        public static Func<CultureInfo, TDetector> ResolveDetectorByLanguage<TDetector>(IComponentContext c)
        {
            var context = c.Resolve<IComponentContext>();
            
            return language => 
            {
                var baseName = typeof(TDetector).Name.Remove(0, 1);

                return context.ResolveKeyed<TDetector>(language.EnglishName + baseName);
            };
        }
    }
}
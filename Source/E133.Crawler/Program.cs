using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Autofac;

using E133.Business;
using E133.Business.Bases;
using E133.Parser;

namespace E133.Crawler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainAsync().Wait();
        }

        static async Task MainAsync()
        {
			var builder = new ContainerBuilder();
            builder.RegisterModule(new E133.Business.AutofacModule());
            builder.RegisterModule(new E133.Crawler.AutofacModule());
            builder.RegisterModule(new E133.Parser.AutofacModule());
            var container = builder.Build();
            
            // var repo = container.Resolve<IQuickRecipeRepository>();
            var knownCrawlers = container.Resolve<IEnumerable<IHtmlCrawler>>();

            foreach (var crawler in knownCrawlers)
            {
                // TODO Start these assholes asynchronously
                var allSiteLinks = await crawler.GetAllSiteLinks();

                foreach (var link in allSiteLinks) 
                {
                //     var isRecipe = parser.IsRecipePage(link);
                //     if (isRecipe)
                //     {
                //         var recipe = await parser.ParseHtmlAsync(link);
                //         if (recipe != null)
                //         {
                            Uri result = null;
                            if (Uri.TryCreate(crawler.Base.Domain, link, out result))
                            {
                                Console.WriteLine("Parsing of " + result.AbsoluteUri + " was successful. Adding to repo.");
                            }

                //             var success = await repo.InsertAsync(recipe);
                //             if (success)
                //             {
                //                 Console.WriteLine("Recipe " + link + " was successfully added to repo. Removing link.");
                //             }
                //         }
                //         else
                //         {
                //             Console.WriteLine("Parsing of " + link + " was UNsuccessful.");
                //         }
                //     }
                }
            }

            Console.ReadLine();
        }
    }
}

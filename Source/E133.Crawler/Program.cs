using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Autofac;

using E133.Business;
using E133.Database;
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
            MongoDBConfig.RegisterClassMaps();

			var builder = new ContainerBuilder();
            builder.RegisterModule(new E133.Business.AutofacModule());
            builder.RegisterModule(new E133.Crawler.AutofacModule());
            builder.RegisterModule(new E133.Database.AutofacModule());
            builder.RegisterModule(new E133.Parser.AutofacModule());

            var container = builder.Build();
            
            var repo = container.Resolve<IQuickRecipeRepository>();
            var knownCrawlers = container.Resolve<IEnumerable<IHtmlCrawler>>();
            var recipeCount = 0;
            var parserFactory = container.Resolve<IParserFactory>();

            var sw = Stopwatch.StartNew();

            foreach (var crawler in knownCrawlers)
            {
                // TODO Start these assholes asynchronously
                var sw1 = Stopwatch.StartNew();
                var allSiteLinks = await crawler.GetAllSiteLinks();
                Console.WriteLine("Took " + sw1.Elapsed.ToString("c") + " to get all links.");
                sw1.Stop();

                var sw2 = Stopwatch.StartNew();
                foreach (var link in allSiteLinks) 
                {
                    Uri result = null;
                    if (Uri.TryCreate(crawler.Base.Domain, link, out result))
                    {
                        if (await crawler.IsRecipeLink(result))
                        {
                            Console.WriteLine("Found a recipe! " + result.AbsoluteUri);
                            recipeCount++;

                            try 
                            {
                                var recipe = await parserFactory.CreateParser(result).ParseHtmlAsync(result);
                                if (recipe != null)
                                {
                                    var recipeByTitle = await repo.GetByUrlAsync(result.AbsoluteUri);
                                    if (recipeByTitle == null) 
                                    {
                                        var response = await repo.InsertAsync(recipe);
                                        if (response)
                                        {
                                            Console.WriteLine("Recipe was successfully added to repo.");
                                        }
                                        else 
                                        {
                                            Console.WriteLine("Could not add recipe for some reason");
                                        }
                                    }
                                    else 
                                    {
                                        Console.WriteLine("Recipe already exists.");
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Found a problem with recipe " + result.AbsoluteUri + ": " + e.Message);
                            }
                        }
                    }
                }
                Console.WriteLine("Took " + sw2.Elapsed.ToString("c") + " to check recipe links.");
                sw2.Stop();
            }

            sw.Stop();

            Console.WriteLine("All in all, found " + recipeCount + " recipes.");
            Console.WriteLine("Took " + sw.Elapsed.ToString("c") + ".");

            Console.ReadLine();
        }
    }
}

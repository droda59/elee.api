using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using E133.Business;
using E133.Business.Bases;

using HtmlAgilityPack;

namespace E133.Crawler
{
    internal abstract class SiteCrawler<TBase> : IHtmlCrawler, IDisposable
        where TBase : IBase, new()
    {
        private readonly IHtmlLoader _htmlLoader;
        
        protected SiteCrawler(IHtmlLoader htmlLoader)
        {
            this._htmlLoader = htmlLoader;
            this._htmlLoader.Initialize();
            this.Base = new TBase();
        }

        public IBase Base { get; }

        protected virtual IEnumerable<Func<string, bool>> Exclusions 
        {
            get
            {
                return new List<Func<string, bool>> {
                    (link) => link != "/",
                    (link) => !link.Contains("#")
                };
            }
        }

        // Faire un IDictionary<Uri, bool> _links
        // Démarre un acteur-lecteur sur le dictionaire
        // Ajoute le base url dans le dictionaire (acteur démarre ainsi)
        
        // ACTEUR LECTEUR
        // Lire tous les liens de la page
        //// Ajouter chaque lien lu dans le dictionaire
        // Acteur passe à travers le dictionaire
        //// Acteur visite les pages et fait la même chose
        
        // ACTEUR PARSER
        // Vérifie si le lien est une recette
        // Vérifie l'existence de la recette dans la BD
        // Si possible, vérifie la date de dernière modification
        // Parse la recette
        // Ajoute la recette dans la BD

        public void Dispose()
        {
            this._htmlLoader.Dispose();
        }

        public async Task<bool> IsRecipeLink(Uri uri)
        {
            bool isRecipe = false;
            try
            {
                var content = await this._htmlLoader.ReadHtmlAsync(uri);
                if (!string.IsNullOrEmpty(content))
                {
                    var document = new HtmlDocument();
                    document.LoadHtml(System.Net.WebUtility.HtmlDecode(content));
                    
                    isRecipe = document.DocumentNode
                        .SelectSingleNode(".//div[@id='block-system-main']")
                        .SelectSingleNode(".//div[@class='content']")
                        .SelectSingleNode(".//div[contains(@class, 'recipe-detail-page')]") != null;
                }
            }
            catch (System.Exception)
            {
            }

            return isRecipe;
        }

        public async Task<IEnumerable<string>> GetAllSiteLinks()
        {
            var discoveredLinks = new HashSet<string>();
            var unprocessedLinks = new HashSet<Uri>();
            Uri link = this.Base.Domain;

            discoveredLinks.Add(link.AbsolutePath);
            unprocessedLinks.Add(link);
            do
            {
                try
                {
                    await this.GetPageLinks(link, discoveredLinks, unprocessedLinks);
                }
                catch (Exception) 
                {
                }
                finally 
                {
                    // TODO Log bitch
                    unprocessedLinks.Remove(link);
                }

                if (discoveredLinks.Count() >= 500)
                {
                    break;
                }

            } while ((link = unprocessedLinks.FirstOrDefault()) != null);

            return discoveredLinks;
        }
        
        private async Task GetPageLinks(Uri pageUri, HashSet<string> discoveredLinks, HashSet<Uri> unprocessedLinks)
        {
            var content = await this._htmlLoader.ReadHtmlAsync(pageUri);
            if (!string.IsNullOrEmpty(content))
            {
                var document = new HtmlDocument();
                document.LoadHtml(System.Net.WebUtility.HtmlDecode(content));
                
                var linkNodes = document.DocumentNode
                    .SelectNodes(".//a[@href]")
                    .Select(x => x.Attributes["href"].Value)
                    .ToList();
                    
                foreach (var linkNode in linkNodes)
                {
                    if (this.Exclusions.All(exclusion => exclusion(linkNode)))
                    {
                        Uri result = null;
                        var mayAdd = false;
                        if (Uri.TryCreate(linkNode, UriKind.Absolute, out result))
                        {
                            if (pageUri.Authority.Contains(result.Authority))
                            {
                                mayAdd = true;
                            }
                        }
                        else if (Uri.TryCreate(pageUri, linkNode, out result))
                        {
                            mayAdd = true;
                        }

                        if (mayAdd)
                        {
                            var absoluteTrim = result.AbsolutePath.TrimEnd('/');
                            if (!discoveredLinks.Contains(absoluteTrim))
                            {
                                discoveredLinks.Add(absoluteTrim);

                                if (await this.IsRecipeLink(result))
                                {
                                    break;
                                }
                                
                                unprocessedLinks.Add(result);

                                continue;
                            }
                        }
                    }
                }
            }
        }
    }
}
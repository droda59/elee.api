using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using E133.Business.Bases;

namespace E133.Crawler
{
    public interface IHtmlCrawler
    {
        IBase Base { get; }
        
        Task<IEnumerable<string>> GetAllSiteLinks();

        Task<bool> IsRecipeLink(Uri uri);
    }
}
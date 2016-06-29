using System;
using System.Collections.Generic;
using System.Linq;

using E133.Business;
using E133.Business.Bases;

namespace E133.Crawler
{
    internal sealed class RicardoCrawler : SiteCrawler<RicardoBase>
    {
        public RicardoCrawler(IHtmlLoader htmlLoader)
            : base(htmlLoader)
        {
        }

        protected override IEnumerable<Func<string, bool>> Exclusions 
        {
            get
            {
                return new List<Func<string, bool>> {
                    (link) => !link.EndsWith(".pdf"),
                    (link) => !link.EndsWith(".jpg"),
                    (link) => !link.Contains("facebook"),
                    (link) => !link.Contains("javascript"),
                    (link) => !link.Contains("/sort/"),
                    (link) => !link.EndsWith("/full"),
                    (link) => !link.EndsWith("/fr"),
                    (link) => !link.Equals("fr"),
                    (link) => !link.EndsWith("/en"),
                    (link) => !link.Equals("/en"),
                    (link) => !link.StartsWith("/en/")
                }.Concat(base.Exclusions);
            }
        }
    }
}
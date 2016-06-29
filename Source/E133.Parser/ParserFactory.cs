using System;
using System.Collections.Generic;
using System.Linq;

namespace E133.Parser
{
    internal class ParserFactory : IParserFactory
    {
        private readonly IDictionary<string, IHtmlParser> _parsers;

        public ParserFactory(IEnumerable<IHtmlParser> parsers)
        {
            this._parsers = parsers.ToDictionary(x => x.Base.Domain.Authority);
        }

        public IHtmlParser CreateParser(string url)
        {
            var uri = new Uri(url);

            return this.CreateParser(uri);
        }

        public IHtmlParser CreateParser(Uri uri)
        {
            var domain = uri.Authority;
            if (!this._parsers.ContainsKey(domain))
            {
                throw new KeyNotFoundException();
            }

            return this._parsers[domain];
        }
    }
}
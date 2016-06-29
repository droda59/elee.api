using System;

namespace E133.Parser
{
    public interface IParserFactory
    {
        IHtmlParser CreateParser(string url);
        
        IHtmlParser CreateParser(Uri uri);
    }
}
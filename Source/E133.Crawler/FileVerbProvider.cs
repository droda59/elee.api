using System;
using System.Collections.Generic;
using System.IO;

using E133.Parser.LanguageUtilities;

namespace E133.Crawler
{
    internal class FileVerbProvider : IVerbProvider
    {
        private readonly Lazy<HashSet<string>> _acceptedVerbs;

        public FileVerbProvider()
        {
            this._acceptedVerbs = new Lazy<HashSet<string>>(GetVerbs);
        }

        public HashSet<string> AcceptedVerbs 
        { 
            get { return this._acceptedVerbs.Value; }
        }

        private HashSet<string> GetVerbs() 
        {
            var acceptedActions = new HashSet<string>();

            var inputFile = new FileInfo(Directory.GetCurrentDirectory() + "/Resources/FrenchVerbs.txt");
            using (var stream = inputFile.OpenRead())
            {
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        acceptedActions.Add(line);
                    }
                }
            }

            return acceptedActions;
        }
    }
}
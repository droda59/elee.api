using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using Autofac;
using Autofac.Core;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

using E133.Parser;
using E133.Parser.LanguageUtilities;

namespace E133.Api
{
    internal class AspVerbProvider : IVerbProvider
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly Lazy<HashSet<string>> _acceptedVerbs;

        public AspVerbProvider(IHostingEnvironment hostingEnvironment)
        {
            this._hostingEnvironment = hostingEnvironment;
            this._acceptedVerbs = new Lazy<HashSet<string>>(GetVerbs);
        }

        public HashSet<string> AcceptedVerbs 
        { 
            get { return this._acceptedVerbs.Value; }
        }

        private HashSet<string> GetVerbs() 
        {
            var acceptedActions = new HashSet<string>();

            var inputFile = this._hostingEnvironment.WebRootFileProvider.GetFileInfo(@"/Resources/FrenchVerbs.txt");
            using (var stream = inputFile.CreateReadStream())
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
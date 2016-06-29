using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;

namespace E133.Parser.LanguageUtilities.French
{
    internal class FrenchActionDetector : IActionDetector
    {
        private readonly HashSet<string> _acceptedActions;
        private readonly StringIgnoreCaseComparer _stringIgnoreCaseComparer;
        private readonly Regex _wordExpression;

        public FrenchActionDetector(IHostingEnvironment environment)
        {
            this._stringIgnoreCaseComparer = new StringIgnoreCaseComparer();
            this._acceptedActions = new HashSet<string>();
            this._wordExpression = new Regex(@"[\w()�]+['�]*|[,]|[\)]\b", RegexOptions.Compiled);

            var fileInfo = environment.WebRootFileProvider.GetFileInfo(@"/Resources/FrenchVerbs.txt");
            using (var stream = fileInfo.CreateReadStream())
            {
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        this._acceptedActions.Add(line);
                    }
                }
            }
        }

        public bool IsAction(string part)
        {
            if (part.EndsWith("er", StringComparison.OrdinalIgnoreCase)
                || part.EndsWith("ir", StringComparison.OrdinalIgnoreCase)
                || part.EndsWith("ire", StringComparison.OrdinalIgnoreCase)
                || part.EndsWith("tre", StringComparison.OrdinalIgnoreCase)
                || part.EndsWith("vre", StringComparison.OrdinalIgnoreCase)
                || part.EndsWith("dre", StringComparison.OrdinalIgnoreCase))
            {
                return this._acceptedActions.Contains(part, this._stringIgnoreCaseComparer);
            }

            return false;
        }

        public string Actionify(string phrase)
        {
            var actionBuilder = new StringBuilder();

            var words = this._wordExpression.Matches(phrase);
            var index = 0;
            while (index < words.Count)
            {
                string action;
                var word = words[index];
                var endingLength = AnalyzeEndingLength(word.Value);
                var verbGroup = AnalyzeVerbGroup(word.Value);

                switch (verbGroup)
                {
                    case 1:
                        action = word.Value.Substring(0, word.Length - endingLength) + "er";
                        break;
                    case 2:
                        action = word.Value.Substring(0, word.Length - endingLength) + "ir";
                        break;
                    case 3:
                        action = word.Value.Substring(0, word.Length - endingLength) + "re";
                        break;
                    default:
                        action = word.Value;
                        break;
                }

                actionBuilder.AppendFormat("{0} ", action);

                index++;
            }

            return actionBuilder.ToString();
        }

        private static int AnalyzeVerbGroup(string word)
        {
            if (word.EndsWith("�", StringComparison.OrdinalIgnoreCase)
                || word.EndsWith("�s", StringComparison.OrdinalIgnoreCase)
                || word.EndsWith("�e", StringComparison.OrdinalIgnoreCase)
                || word.EndsWith("�es", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (word.EndsWith("i", StringComparison.OrdinalIgnoreCase) 
                || word.EndsWith("is", StringComparison.OrdinalIgnoreCase)
                || word.EndsWith("ie", StringComparison.OrdinalIgnoreCase) 
                || word.EndsWith("ies", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            if (word.EndsWith("u", StringComparison.OrdinalIgnoreCase) 
                || word.EndsWith("us", StringComparison.OrdinalIgnoreCase)
                || word.EndsWith("ue", StringComparison.OrdinalIgnoreCase) 
                || word.EndsWith("ues", StringComparison.OrdinalIgnoreCase))
            {
                return 3;
            }

            return 0;
        }

        private static int AnalyzeEndingLength(string word)
        {
            if (word.EndsWith("�", StringComparison.OrdinalIgnoreCase) 
                || word.EndsWith("i", StringComparison.OrdinalIgnoreCase)
                || word.EndsWith("u", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }
            
            if (word.EndsWith("�s", StringComparison.OrdinalIgnoreCase) 
                || word.EndsWith("�e", StringComparison.OrdinalIgnoreCase)
                || word.EndsWith("is", StringComparison.OrdinalIgnoreCase) 
                || word.EndsWith("ie", StringComparison.OrdinalIgnoreCase)
                || word.EndsWith("us", StringComparison.OrdinalIgnoreCase) 
                || word.EndsWith("ue", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }
            
            if (word.EndsWith("�es", StringComparison.OrdinalIgnoreCase) 
                || word.EndsWith("ies", StringComparison.OrdinalIgnoreCase)
                || word.EndsWith("ues", StringComparison.OrdinalIgnoreCase))
            {
                return 3;
            }

            return 0;
        }
    }
}
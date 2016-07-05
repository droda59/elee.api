using System;
using System.Threading.Tasks;

using E133.Business.Bases;
using E133.Business.Models;

namespace E133.Parser
{
    public interface IHtmlParser
    {
        IBase Base { get; }

        Task<QuickRecipe> ParseHtmlAsync(Uri uri);
    }
}
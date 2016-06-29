using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using E133.Business.Models;
using E133.Parser;

using Microsoft.AspNetCore.Mvc;

namespace E133.Api.Controllers
{
    // [Authorize(Policy = "LocalAuthorizationOnly")]
    [Route("api/[controller]")]
    public class ParserTestController : Controller
    {
        private readonly IParserFactory _parserFactory;

        public ParserTestController(IParserFactory parserFactory)
        {
            this._parserFactory = parserFactory;
        }

        [HttpGet]
        [Route("parse")]
        public async Task<IActionResult> ParseAsync(string url)
        {
            var result = await this.ParseRecipeAsync(url);
            if (result == null)
            {
                return new BadRequestResult();
            }

            return new ObjectResult(result);
        }

        private async Task<QuickRecipe> ParseRecipeAsync(string url)
        {
            var uri = new Uri(url);
            IHtmlParser parser = null;

            try
            {
                parser = this._parserFactory.CreateParser(url);
            }
            catch (KeyNotFoundException)
            {
                return null;
            }

            var parsedContent = await parser.ParseHtmlAsync(uri);

            return parsedContent;
        }
    }
}
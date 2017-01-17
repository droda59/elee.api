using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using E133.Parser;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E133.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Policy = "LocalOnly")]
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
            var uri = new Uri(url);
            IHtmlParser parser = null;

            try
            {
                parser = this._parserFactory.CreateParser(url);
            }
            catch (KeyNotFoundException)
            {
                return new BadRequestResult();
            }

            var parsedContent = await parser.ParseHtmlAsync(uri);

            return new ObjectResult(parsedContent);
        }
    }
}
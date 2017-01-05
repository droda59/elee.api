using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using E133.Business;
using E133.Business.Models;

namespace E133.Api.Controllers
{
    [Route("api/[controller]")]
    public class ContactFormController : Controller
    {
        private readonly IContactFormRepository _repo;

        public ContactFormController(IContactFormRepository repo)
        {
            this._repo = repo;
        }

        [HttpGet]
        public async Task<IEnumerable<ContactForm>> Get()
        {
            return await this._repo.GetAsync();
        }

        [HttpPost]
        public async Task<bool> Post([FromBody]ContactForm contactForm)
        {
            return await this._repo.InsertAsync(contactForm);
        }
    }
}

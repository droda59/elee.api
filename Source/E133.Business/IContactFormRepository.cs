using System.Collections.Generic;
using System.Threading.Tasks;

using E133.Business.Models;

namespace E133.Business
{
    public interface IContactFormRepository
    {
        Task<IEnumerable<ContactForm>> GetAsync();

        Task<bool> InsertAsync(ContactForm contactForm);
    }
}
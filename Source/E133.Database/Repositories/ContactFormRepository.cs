using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using E133.Business;
using E133.Business.Models;

using MongoDB.Bson;
using MongoDB.Driver;

namespace E133.Database.Repositories
{
    internal class ContactFormRepository : EntityRepository<ContactForm>, IContactFormRepository
    {
        public ContactFormRepository(IOptions<MongoDBOptions> options)
            : base(options) 
        {
        }
        
        public async Task<IEnumerable<ContactForm>> GetAsync()
        {
            var builder = Builders<ContactForm>.Filter;
            var filter = new BsonDocument();

            var results = await this.Collection.Find(filter).ToListAsync();
            return results;
        }

        public async Task<bool> InsertAsync(ContactForm contactForm)
        {
            await this.Collection.InsertOneAsync(contactForm);

            return true;
        }
    }
}
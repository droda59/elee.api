using Microsoft.Extensions.Options;

using MongoDB.Driver;

namespace E133.Database
{
    public abstract class EntityRepository<TDocument>
    {
        private readonly IMongoCollection<TDocument> _collection;

        protected EntityRepository(IOptions<MongoDBOptions> options)
        {
            var client = new MongoClient(options.Value.ConnectionString);
            var database = client.GetDatabase(options.Value.DatabaseName);

            this._collection = database.GetCollection<TDocument>(typeof(TDocument).Name.ToLower());
        }

        protected IMongoCollection<TDocument> Collection
        {
            get { return this._collection; }
        }
    }
}
using MongoDB.Driver;

namespace E133.Database
{
    public abstract class EntityRepository<TDocument>
    {
        private readonly IMongoCollection<TDocument> _collection;

        protected EntityRepository()
        {
            var client = new MongoClient("mongodb://admin:e133@ds064748.mlab.com:64748/e133");
            var database = client.GetDatabase("e133");

            this._collection = database.GetCollection<TDocument>(typeof(TDocument).Name.ToLower());
        }

        protected IMongoCollection<TDocument> Collection
        {
            get { return this._collection; }
        }
    }
}
using E133.Business;
using E133.Business.Models;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace E133.Database
{
    public static class MongoDBConfig
    {
        public static void RegisterClassMaps()
        {
            BsonClassMap.RegisterClassMap<Document>(
                x =>
                    {
                        x.AutoMap();
                        x.MapIdMember(y => y.Id)
                            .SetIdGenerator(StringObjectIdGenerator.Instance);
                    });

            BsonClassMap.RegisterClassMap<Part>(
                x =>
                    {
                        x.AutoMap();
                        x.AddKnownType(typeof(ActionPart));
                        x.AddKnownType(typeof(TextPart));
                        x.AddKnownType(typeof(TimerPart));
                        x.AddKnownType(typeof(IngredientPart));
                        x.AddKnownType(typeof(IngredientEnumerationPart));
                    });

            BsonClassMap.RegisterClassMap<Quantity>(
                x =>
                    {
                        x.AutoMap();
                        x.MapMember(y => y.Abbreviation)
                            .SetSerializer(new EnumSerializer<MeasureUnit>(BsonType.String));
                    });
        }
    }
}
using E133.Business;
using E133.Business.Models;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace E133.Database
{
    public static class MongoDBConfig
    {
        public static void RegisterClassMaps()
        {
            var conventionPack = new ConventionPack();
            conventionPack.Add(new CamelCaseElementNameConvention());
            ConventionRegistry.Register("camelCase", conventionPack, t => true);

            BsonClassMap.RegisterClassMap<Document>(
                x =>
                    {
                        x.AutoMap();
                        x.MapIdMember(y => y.Id)
                            .SetSerializer(new StringSerializer(BsonType.ObjectId))
                            .SetIdGenerator(StringObjectIdGenerator.Instance);
                    });
            
            BsonClassMap.RegisterClassMap<Subrecipe>(
                x => 
                    {
                        x.AutoMap();
                        x.MapProperty(c => c.SubrecipeId).SetElementName("id");
                    });
            
            BsonClassMap.RegisterClassMap<Step>(
                x => 
                    {
                        x.AutoMap();
                        x.MapProperty(c => c.StepId).SetElementName("id");
                    });
            
            BsonClassMap.RegisterClassMap<Ingredient>(
                x => 
                    {
                        x.AutoMap();
                        x.MapProperty(c => c.IngredientId).SetElementName("id");
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
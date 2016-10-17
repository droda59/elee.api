using System;

using E133.Business.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace E133.Business.Serialization
{
    internal class PartConverter : JsonConverter
    {
        private static JsonSerializerSettings SpecifiedSubclassConversion = new JsonSerializerSettings { ContractResolver = new PartSpecifiedConcreteClassConverter() };

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Part);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            switch (jObject["type"].Value<string>())
            {
                case "ingredient": return JsonConvert.DeserializeObject<IngredientPart>(jObject.ToString(), SpecifiedSubclassConversion);
                case "quantity": return JsonConvert.DeserializeObject<QuantityOfIngredientPart>(jObject.ToString(), SpecifiedSubclassConversion);
                case "action": return JsonConvert.DeserializeObject<ActionPart>(jObject.ToString(), SpecifiedSubclassConversion); 
                case "enumeration": return JsonConvert.DeserializeObject<EnumerationPart>(jObject.ToString(), SpecifiedSubclassConversion); 
                case "text": return JsonConvert.DeserializeObject<TextPart>(jObject.ToString(), SpecifiedSubclassConversion); 
                case "timer": return JsonConvert.DeserializeObject<TimerPart>(jObject.ToString(), SpecifiedSubclassConversion); 
                default: throw new Exception();
            }

            throw new NotImplementedException();
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
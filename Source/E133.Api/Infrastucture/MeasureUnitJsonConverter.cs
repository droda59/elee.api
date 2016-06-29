using System;

using E133.Business;

using Newtonsoft.Json;

namespace E133.Api.Infrastructure
{
    public class MeasureUnitJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(Convert((MeasureUnit)value));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(MeasureUnit);
        }

        private static string Convert(MeasureUnit measureUnit)
        {
            switch (measureUnit)
            {
                case MeasureUnit.Millilitre: return "ml";
                case MeasureUnit.Centilitre: return "cl";
                case MeasureUnit.Decilitre: return "dl";
                case MeasureUnit.Litre: return "l";
                case MeasureUnit.Teaspoon: return "tsp";
                case MeasureUnit.Tablespoon: return "tbsp";
                case MeasureUnit.Ounce: return "oz";
                case MeasureUnit.Cup: return "cups";
                case MeasureUnit.Unit: return "unit";
                case MeasureUnit.Pinch: return "pinch";
                case MeasureUnit.Gram: return "g";
                case MeasureUnit.Kilogram: return "kg";
                case MeasureUnit.Pound: return "lb";
                default: 
                    return string.Empty;
            }
        }
    }
}

using System;

using Newtonsoft.Json;

namespace E133.Business.Serialization
{
    internal class MeasureUnitJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(Convert((MeasureUnit)value));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var enumString = (string)reader.Value;
            MeasureUnit enumValue;

            if (Enum.TryParse<MeasureUnit>(enumString, out enumValue))
            {
                return enumValue;
            }
            else 
            {
                switch (enumString)
                {
                    case "ml": return MeasureUnit.Millilitre;
                    case "cl": return MeasureUnit.Centilitre;
                    case "dl": return MeasureUnit.Decilitre;
                    case "l": return MeasureUnit.Litre;
                    case "tsp": return MeasureUnit.Teaspoon;
                    case "tbsp": return MeasureUnit.Tablespoon;
                    case "oz": return MeasureUnit.Ounce;
                    case "cup": return MeasureUnit.Cup;
                    case "unit": return MeasureUnit.Unit;
                    case "pinch": return MeasureUnit.Pinch;
                    case "g": return MeasureUnit.Gram;
                    case "kg": return MeasureUnit.Kilogram;
                    case "lb": return MeasureUnit.Pound;
                    default: 
                        return null;
                }
            }
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
                case MeasureUnit.Cup: return "cup";
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

using E133.Business.Serialization;

using Newtonsoft.Json;

namespace E133.Business
{
    [JsonConverter(typeof(MeasureUnitJsonConverter))]
    public enum MeasureUnit
    {
        Millilitre, 
        Centilitre, 
        Decilitre,
        Litre, 

        Teaspoon,
        Tablespoon,
        Ounce,
        Cup,

        Unit, 
        Pinch, 

        Gram,
        Kilogram,
        Pound
    }
}
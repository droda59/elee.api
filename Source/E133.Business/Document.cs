using Newtonsoft.Json;

namespace E133.Business
{
    public abstract class Document
    {
        [JsonProperty("_id")]
        public string Id { get; set; }
    }
}

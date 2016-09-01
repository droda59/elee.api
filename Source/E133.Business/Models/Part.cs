using System.Diagnostics;

using E133.Business.Serialization;

using Newtonsoft.Json;

namespace E133.Business.Models
{
    [DebuggerDisplay("{DebuggerDisplay}")]
    [JsonConverter(typeof(PartConverter))]
    public abstract class Part
    {
        public abstract string Type { get; set; }

        internal abstract string DebuggerDisplay { get; }
    }
}
using Newtonsoft.Json;

namespace Cod2GSI
{
    public class Cod2Event
    {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("game")]
        public string Game { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("addedScore")]
        public int AddedScore { get; set; }

        [JsonProperty("guid")]
        public string Guid { get; set; }

        [JsonProperty("adress")]
        public string Adress { get; set; }
    }
}

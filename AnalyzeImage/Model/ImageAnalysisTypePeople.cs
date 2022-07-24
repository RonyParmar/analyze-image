using Newtonsoft.Json;
using System;

namespace RonyParm.Azure.AnalyzeImage
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ImageAnalysisTypePeople
    {
        [JsonProperty(Required = Required.Always)]
        public string id;

        [JsonProperty(Required = Required.Always)]
        public string blobUrl;

        [JsonProperty(Required = Required.Always)]
        public int totalPeople;

        [JsonProperty(Required = Required.Always)]
        public string type;

        [JsonProperty(Required = Required.Always)]
        public DateTime createdDateTime;
    }
}

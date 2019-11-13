using Newtonsoft.Json;
using System;

namespace AlexaBackendAPI.Graph.Models
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Calendar : DirectoryObject
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "objectType", Required = Required.Default)]
        public string ObjectType { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "name", Required = Required.Default)]
        public string Name { get; set; }

    }
}

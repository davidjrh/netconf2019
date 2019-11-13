using Newtonsoft.Json;
using System;

namespace AlexaBackendAPI.Graph.Models
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class CalendarView : DirectoryObject
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "objectType", Required = Required.Default)]
        public string ObjectType { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "subject", Required = Required.Default)]
        public string Subject { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "start", Required = Required.Default)]
        public CalendarDateTime Start { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "end", Required = Required.Default)]
        public CalendarDateTime End { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "location", Required = Required.Default)]
        public Location Location { get; set; }
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class CalendarDateTime
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "dateTime", Required = Required.Default)]
        public DateTime DateTime { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "timeZone", Required = Required.Default)]
        public string TimeZone { get; set; }
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Location
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "displayName", Required = Required.Default)]
        public string DisplayName { get; set; }
    }
}

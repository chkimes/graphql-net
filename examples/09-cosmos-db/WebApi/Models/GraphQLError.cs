using Newtonsoft.Json;
using System.Collections.Generic;

namespace WebApi.Models
{
    public class GraphQLError
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("locations")]
        public IEnumerable<GraphQLLocation> Locations { get; set; }
    }
}

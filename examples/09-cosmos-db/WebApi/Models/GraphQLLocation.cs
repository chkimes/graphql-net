using Newtonsoft.Json;

namespace WebApi.Models
{
    /// <summary>
    /// Describes the location of a GraphQL error.
    /// </summary>
    public class GraphQLLocation
    {
        /// <summary>
        /// 
        /// TODO: Must be above 1
        /// </summary>
        [JsonProperty("line")]
        public int Line { get; set; }

        /// <summary>
        /// 
        /// TODO: Must be above 1
        /// </summary>
        [JsonProperty("column")]
        public int Column { get; set; }
    }
}

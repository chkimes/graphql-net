using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApi.Models
{
    /// <summary>
    /// GraphQL Response object as defined in specification.
    /// Specification: http://facebook.github.io/graphql/October2016/#sec-Response-Format
    /// </summary>
    public class GraphQLResponse
    {
        /// <summary>
        /// Data property.
        /// TODO: Only show if execution started.
        /// </summary>
        [JsonProperty("data")]
        public IDictionary<string, object> Data { get; set; }

        /// <summary>
        /// Errors property.
        /// TODO: Must be present whenever errors were encountered during execution.
        /// "If the data entry in the response is null or not present, the errors entry in the response must not be empty. 
        /// It must contain at least one error. The errors it contains should indicate why no data was able to be returned."
        /// </summary>
        [JsonProperty("errors", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<string> Errors { get; set; }
    }
}

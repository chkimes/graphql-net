using Newtonsoft.Json;

namespace WebApi.Tests.Models
{
    public class GraphQLResponse<TData>
    {
        [JsonProperty("data")]
        public TData Data { get; set; }
    }
}

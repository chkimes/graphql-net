using Newtonsoft.Json;

namespace WebApi.Models
{
    public class Account
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        public bool Paid { get; set; }
    }
}

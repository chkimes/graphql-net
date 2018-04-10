using Newtonsoft.Json;

namespace WebApi.Models
{
    public class User
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "profile")]
        public Profile Profile { get; set; }

        [JsonProperty(PropertyName = "accountId")]
        public string AccountId { get; set; }
        [JsonProperty(PropertyName = "account")]
        public Account Account { get; set; }
    }
}

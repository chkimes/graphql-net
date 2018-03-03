using Newtonsoft.Json;

namespace WebApi.Models
{
    public class Profile
    {
        [JsonProperty(PropertyName = "age")]
        public int Age { get; set; }

        [JsonProperty(PropertyName = "gender")]
        public string Gender { get; set; }
    }
}

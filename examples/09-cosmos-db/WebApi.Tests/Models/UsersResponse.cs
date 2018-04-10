using Newtonsoft.Json;
using WebApi.Models;

namespace WebApi.Tests.Models
{
    public class UsersResponse
    {
        [JsonProperty("users")]
        public User[] Users { get; set; } 
    }
}

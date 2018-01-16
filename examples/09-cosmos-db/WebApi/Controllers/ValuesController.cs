using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApi.Models;
using WebApi.Services;
using GraphQL.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : ControllerBase
    {
        // Place to store the Config object and use in this controller
        private readonly IConfiguration config;
        private readonly IMyDocumentClient documentClient;
        private readonly IGraphQL gql;

        // Constructor that that takes IConfiguration is called on instantiation thanks to Dependency injection
        public ValuesController(IConfiguration config, IMyDocumentClient documentClient, IGraphQL gql)
        {
            this.config = config;
            this.documentClient = documentClient;
            this.gql = gql;
        }

        // GET api/values
        [HttpGet]
        public async Task<IEnumerable<User>> Get()
        {
            return (await documentClient.GetUsersIQueryableAsync()).ToArray();
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            var q1 = @"{
                        users {
                            id,
                            userId,
                            profile
                        }";

            var q2 = @"{
                        user(id: ""asdf"") {
                            id,
                            userId,
                            profile
                        }
                      }";

            var q3 = @"{
                        totalUsers
                      }";

            var queryResult = gql.Current.ExecuteQuery(q3);

            return JsonConvert.SerializeObject(queryResult, Formatting.Indented);
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers
{
    /// <summary>
    /// GraphQL api endpoint as per specification.
    /// Specification: http://graphql.org/learn/serving-over-http/
    /// </summary>
    [Route("[controller]")]
    [Produces("application/json")]
    public class GraphQLController : ControllerBase
    {
        private readonly IGraphQL gql;

        public GraphQLController(IGraphQL gql)
        {
            this.gql = gql;
        }

        /// <summary>
        /// Support for a GET request with a "query" param passed in the query string.
        /// </summary>
        /// <param name="query">The GraphQL query to execute.</param>
        /// <returns>The execution result.</returns>
        [HttpGet]
        public GraphQLResponse Get(string query)
        {
            // TODO: Try catch and handle errors by adding to response
            var data = gql.Current.ExecuteQuery(query);

            return new GraphQLResponse()
            {
                Data = data
            };
        }

        /// <summary>
        /// Support for a POST request with a Content-Type header of "application/graphql"
        /// with the HTTP POST body contents as the query to execute.
        /// </summary>
        /// <returns>The execution result.</returns>
        [HttpPost]
        [Consumes("application/graphql")]
        public async Task<GraphQLResponse> PostWithContentType()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                return Get(await reader.ReadToEndAsync());
            }
        }

        /// <summary>
        /// Support for a POST request with JSON-encoded HTTP POST contents in form:
        ///     {
        ///       "query": "...",
        ///       "operationName": "...",
        ///       "variables": { "myVariable": "someValue", ... }
        ///     }
        /// </summary>
        /// <returns>The execution result.</returns>
        [HttpPost]
        [Consumes("application/json")]
        public GraphQLResponse Post([FromBody]GraphQLRequest request)
        {
            // TODO: Handle other properties in the object??
            // TODO: Try catch and handle errors by adding to response
            var data = gql.Current.ExecuteQuery(request.Query);

            return new GraphQLResponse()
            {
                Data = data
            };
        }

        /// <summary>
        /// Support for a POST request with a "query" param passed in the query string.
        /// </summary>
        /// <param name="query">The GraphQL query to execute.</param>
        /// <returns>The execution result.</returns>
        [HttpPost]
        public GraphQLResponse PostWithQueryString([FromQuery]string query)
        {
            return Get(query);
        }
    }
}

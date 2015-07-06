using System.Collections.Generic;
using System.Data.Entity;

namespace EntityFramework.GraphQL
{
    public class Query
    {
        public string Name { get; set; }
        public List<Field> Fields { get; set; }
    }

    public class Field
    {
        public string Name { get; set; }
        public string Alias { get; set; }
        public List<Field> Fields { get; set; }
    }
}
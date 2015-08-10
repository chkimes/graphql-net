using System.Collections.Generic;

namespace GraphQL.Net
{
    public class Query
    {
        public string Name { get; set; }
        public List<Input> Inputs { get; set; }
        public List<Field> Fields { get; set; }
    }

    public class Field
    {
        public string Name { get; set; }
        public string Alias { get; set; }
        public List<Field> Fields { get; set; }
    }

    public class Input
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }
}
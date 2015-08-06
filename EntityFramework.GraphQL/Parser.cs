using System;
using System.Collections.Generic;
using System.Linq;

namespace EntityFramework.GraphQL
{
    public static class Parser
    {
        public static Query Parse(string query)
        {
            var parsed = GraphQLParser.parse(query);
            if (parsed.Value == null)
                throw new Exception("i dunno man");
            if (!parsed.Value.IsQueryOperation)
                throw new NotSupportedException("Only Query operations are currently supported");

            var op = (GraphQLParser.Definition.QueryOperation)parsed.Value;
            return new Query
            {
                Name = op.Item.Item1,
                Inputs = GetInputs(op.Item.Item2),
                Fields = WalkSelection(op.Item.Item3)
            };
        }

        private static List<Input> GetInputs(IEnumerable<Tuple<string, GraphQLParser.Input>> inputs)
        {
            return inputs.Select(i => new Input
            {
                Name = i.Item1,
                Value = GetInputValue(i.Item2)
            }).ToList();
        }

        private static object GetInputValue(GraphQLParser.Input input)
        {
            if (input.IsBoolean) return ((GraphQLParser.Input.Boolean)input).Item;
            if (input.IsFloat) return ((GraphQLParser.Input.Float)input).Item;
            if (input.IsInt) return ((GraphQLParser.Input.Int)input).Item;
            if (input.IsString) return ((GraphQLParser.Input.String)input).Item;
            throw new Exception("Shouldn't be here");
        }

        private static List<Field> WalkSelection(IEnumerable<GraphQLParser.Selection> selection)
        {
            return selection.Select(f => new Field
            {
                Name = f.name,
                Alias = f.alias?.Value ?? f.name,
                Fields = WalkSelection(f.selectionSet)
            }).ToList();
        }
    }
}

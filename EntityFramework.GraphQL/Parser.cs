using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.FSharp.Collections;

namespace EntityFramework.GraphQL
{
    public class Parser
    {
        public Query Parse(string query)
        {
            var parsed = GraphQLParser.parse(query);
            if (parsed.Value == null)
                throw new Exception("i dunno man");
            if (!parsed.Value.IsQueryOperation)
                throw new Exception("i dunno man");

            var op = parsed.Value as GraphQLParser.Definition.QueryOperation;
            var name = op.Item.Item1;
            var selection = op.Item.Item2;
            return new Query
            {
                Name = op.Item.Item1,
                Inputs = GetInputs(op.Item.Item2),
                Fields = WalkSelection(op.Item.Item3)
            };
        }

        private List<Input> GetInputs(FSharpList<Tuple<string, GraphQLParser.Input>> inputs)
        {
            return inputs.Select(i => new Input
            {
                Name = i.Item1,
                Value = GetInputValue(i.Item2)
            }).ToList();
        }

        private object GetInputValue(GraphQLParser.Input input)
        {
            if (input.IsBoolean) return ((GraphQLParser.Input.Boolean)input).Item;
            else if (input.IsFloat) return ((GraphQLParser.Input.Float)input).Item;
            else if (input.IsInt) return ((GraphQLParser.Input.Int)input).Item;
            else if (input.IsString) return ((GraphQLParser.Input.String)input).Item;
            else throw new Exception("Shouldn't be here");
        }

        private List<Field> WalkSelection(FSharpList<GraphQLParser.Selection> selection)
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

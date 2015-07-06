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
                Fields = WalkSelection(op.Item.Item2)
            };
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

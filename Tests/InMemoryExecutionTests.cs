using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Net;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class InMemoryExecutionTests
    {
        [Test] public void LookupSingleEntity() => GenericTests.LookupSingleEntity(MemContext.CreateDefaultContext());
        [Test] public void AliasOneField() => GenericTests.AliasOneField(MemContext.CreateDefaultContext());
        [Test] public void NestedEntity() => GenericTests.NestedEntity(MemContext.CreateDefaultContext());
        [Test] public void NoUserQueryReturnsNull() => GenericTests.NoUserQueryReturnsNull(MemContext.CreateDefaultContext());
        [Test] public void CustomFieldSubQuery() => GenericTests.CustomFieldSubQuery(MemContext.CreateDefaultContext());
        [Test] public void CustomFieldSubQueryUsingContext() => GenericTests.CustomFieldSubQueryUsingContext(MemContext.CreateDefaultContext());
        [Test] public void List() => GenericTests.List(MemContext.CreateDefaultContext());
        [Test] public void ListTypeIsList() => GenericTests.ListTypeIsList(MemContext.CreateDefaultContext());
        [Test] public void NestedEntityList() => GenericTests.NestedEntityList(MemContext.CreateDefaultContext());
        [Test] public void PostField() => GenericTests.PostField(MemContext.CreateDefaultContext());
        [Test] public void PostFieldSubQuery() => GenericTests.PostFieldSubQuery(MemContext.CreateDefaultContext());
        [Test] public void TypeName() => GenericTests.TypeName(MemContext.CreateDefaultContext());
        [Test] public void DateTimeFilter() => GenericTests.DateTimeFilter(MemContext.CreateDefaultContext());
        [Test] public void EnumerableSubField() => GenericTests.EnumerableSubField(MemContext.CreateDefaultContext());
        [Test] public void EnumFieldQuery() => GenericTests.EnumFieldQuery(MemContext.CreateDefaultContext());
        [Test] public void SimpleMutation() => GenericTests.SimpleMutation(MemContext.CreateDefaultContext());
        [Test] public void MutationWithReturn() => GenericTests.MutationWithReturn(MemContext.CreateDefaultContext());
        [Test] public void NullPropagation() => GenericTests.NullPropagation(MemContext.CreateDefaultContext());
        [Test] public void GuidField() => GenericTests.GuidField(MemContext.CreateDefaultContext());
        [Test] public void GuidParameter() => GenericTests.GuidParameter(MemContext.CreateDefaultContext());
        [Test] public void ByteArrayParameter() => GenericTests.ByteArrayParameter(MemContext.CreateDefaultContext());
        [Test] public void ChildListFieldWithParameters() => GenericTests.ChildListFieldWithParameters(MemContext.CreateDefaultContext());
        [Test] public void ChildFieldWithParameters() => GenericTests.ChildFieldWithParameters(MemContext.CreateDefaultContext());
        [Test] public static void Fragments() => GenericTests.Fragments(MemContext.CreateDefaultContext());
        [Test] public static void InlineFragments() => GenericTests.InlineFragments(MemContext.CreateDefaultContext());
        [Test] public static void InlineFragmentWithListField() => GenericTests.InlineFragmentWithListField(MemContext.CreateDefaultContext());
        [Test] public static void FragmentWithMultiLevelInheritance() => GenericTests.FragmentWithMultiLevelInheritance(MemContext.CreateDefaultContext());
        [Test] public static void InlineFragmentWithoutTypenameField() => GenericTests.InlineFragmentWithoutTypenameField(MemContext.CreateDefaultContext());
        [Test] public static void FragmentWithoutTypenameField() => GenericTests.FragmentWithoutTypenameField(MemContext.CreateDefaultContext());
        [Test] public static void InlineFragmentWithoutTypenameFieldWithoutOtherFields() => GenericTests.InlineFragmentWithoutTypenameFieldWithoutOtherFields(MemContext.CreateDefaultContext());
        [Test] public static void FragmentWithMultipleTypenameFields() => GenericTests.FragmentWithMultipleTypenameFields(MemContext.CreateDefaultContext());
        [Test] public static void FragmentWithMultipleTypenameFieldsMixedWithInlineFragment() => GenericTests.FragmentWithMultipleTypenameFieldsMixedWithInlineFragment(MemContext.CreateDefaultContext());

        [Test]
        public void AddAllFields()
        {
            var schema = GraphQL<MemContext>.CreateDefaultSchema(() => new MemContext());
            schema.AddType<User>().AddAllFields();
            schema.AddType<Account>().AddAllFields();
            schema.AddField("user", new { id = 0 }, (db, args) => db.Users.AsQueryable().FirstOrDefault(u => u.Id == args.id));
            schema.Complete();

            var gql = new GraphQL<MemContext>(schema);
            var results = gql.ExecuteQuery("{ user(id:1) { id, name } }");
            Test.DeepEquals(results, "{ user: { id: 1, name: 'Joe User' } }");
        }
    }
}

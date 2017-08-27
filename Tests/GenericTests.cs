using System;
using System.Collections.Generic;
using GraphQL.Net;
using NUnit.Framework;

namespace Tests
{
    public static class GenericTests
    {
        public static void LookupSingleEntity<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ user(id:1) { id, name } }");
            Test.DeepEquals(results, "{ user: { id: 1, name: 'Joe User' } }");
        }

        public static void AliasOneField<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ user(id:1) { idAlias : id, name } }");
            Test.DeepEquals(results, "{ user: { idAlias: 1, name: 'Joe User' } }");
        }

        public static void NestedEntity<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ user(id:1) { id, account { id, name } } }");
            Test.DeepEquals(results, "{ user: { id: 1, account: { id: 1, name: 'My Test Account' } } }");
        }

        public static void NoUserQueryReturnsNull<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ user(id:0) { id, account { id, name } } }");
            Test.DeepEquals(results, "{ user: null }");
        }

        public static void CustomFieldSubQuery<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ user(id:1) { id, accountPaid } }");
            Test.DeepEquals(results, "{ user: { id: 1, accountPaid: true } }");
        }

        public static void CustomFieldSubQueryUsingContext<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ user(id:1) { id, total } }");
            Test.DeepEquals(results, "{ user: { id: 1, total: 2 } }");
        }

        public static void List<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ users { id, name } }");
            Test.DeepEquals(results, "{ users: [{ id: 1, name: 'Joe User'}, { id: 2, name: 'Late Paying User' }] }");
        }

        public static void ListTypeIsList<TContext>(GraphQL<TContext> gql)
        {
            var users = gql.ExecuteQuery("{ users { id, name } }")["users"];
            Assert.AreEqual(users.GetType(), typeof(List<IDictionary<string, object>>));
        }

        public static void NestedEntityList<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ account(id:1) { id, users { id, name } } }");
            Test.DeepEquals(results, "{ account: { id: 1, users: [{ id: 1, name: 'Joe User' }] } }");
        }

        public static void PostField<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ user(id:1) { id, abc } }");
            Test.DeepEquals(results, "{ user: { id: 1, abc: 'easy as 123' } }");
        }

        public static void PostFieldSubQuery<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ user(id:1) { sub { id } } }");
            Test.DeepEquals(results, "{ user: { sub: { id: 1 } } }");
        }

        public static void TypeName<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ user(id:1) { id, __typename } }");
            Test.DeepEquals(results, "{ user: { id: 1, __typename: 'User' } }");
        }

        public static void DateTimeFilter<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ accountPaidBy(paid: { year: 2016 month: 1 day: 1 }) { id } }");
            Test.DeepEquals(results, "{ accountPaidBy: { id: 1 } }");
        }

        public static void EnumerableSubField<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ account(id:1) { activeUsers { id, name } } }");
            Test.DeepEquals(results, "{ account: { activeUsers: [{ id: 1, name: 'Joe User' }] } }");

            var results2 = gql.ExecuteQuery("{ account(id:2) { activeUsers { id, name } } }");
            Test.DeepEquals(results2, "{ account: { activeUsers: [] } }");
        }

        public static void SimpleMutation<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("mutation { mutate(id:1,newVal:5) { id, value } }");
            Test.DeepEquals(results, "{ mutate: { id: 1, value: 5 } }");

            var results2 = gql.ExecuteQuery("mutation { mutate(id:1,newVal:123) { id, value } }");
            Test.DeepEquals(results2, "{ mutate: { id: 1, value: 123 } }");
        }

        public static void MutationWithReturn<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("mutation { addMutate(newVal: 7) { value } }");
            Test.DeepEquals(results, "{ addMutate: { value: 7 } }");
        }

        public static void NullPropagation<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ user(id:1) { id, nullRef { id } } }");
            Test.DeepEquals(results, "{ user: { id: 1, nullRef: null } }");
        }

        public static void GuidField<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ account(id:1) { id, someGuid } }");
            Test.DeepEquals(results, "{ account: { id: 1, someGuid: '00000000-0000-0000-0000-000000000000' } }");
        }

        public static void GuidParameter<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ accountsByGuid(guid:\"00000000-0000-0000-0000-000000000000\") { id, someGuid } }");
            Test.DeepEquals(results, @"{
                                           accountsByGuid: [
                                               { id: 1, someGuid: '00000000-0000-0000-0000-000000000000' },
                                               { id: 2, someGuid: '00000000-0000-0000-0000-000000000000' },
                                           ]
                                       }");
        }

        public static void EnumFieldQuery<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ accountsByType(accountType:accountType_Gold) { id, accountType } }");
            Test.DeepEquals(results, @"{ 
                                            accountsByType: [
                                                    { id: 1, accountType: 'Gold' }
                                            ]
                                        }");
        }

        public static void ByteArrayParameter<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ account(id:1) { id, byteArray } }");
            Test.DeepEquals(results, "{ account: { id: 1, byteArray: 'AQIDBA==' } }"); // [1, 2, 3, 4] serialized to base64 by Json.NET
        }

        public static void ChildListFieldWithParameters<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ account(id:1) { id, name, usersWithActive(active:true) { id, name } } }");
            Test.DeepEquals(results, "{ account: { id: 1, name: 'My Test Account', usersWithActive: [{ id: 1, name: 'Joe User' }] } }");

            results = gql.ExecuteQuery("{ account(id:1) { id, name, usersWithActive(active:false) { id, name } } }");
            Test.DeepEquals(results, "{ account: { id: 1, name: 'My Test Account', usersWithActive: [] } }");
        }

        public static void ChildFieldWithParameters<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ account(id:1) { id, name, firstUserWithActive(active:true) { id, name } } }");
            Test.DeepEquals(results, "{ account: { id: 1, name: 'My Test Account', firstUserWithActive: { id: 1, name: 'Joe User' } } }");

            results = gql.ExecuteQuery("{ account(id:1) { id, name, firstUserWithActive(active:false) { id, name } } }");
            Test.DeepEquals(results, "{ account: { id: 1, name: 'My Test Account', firstUserWithActive: null } }");
        }

        public static void Fragments<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery(
                "{ heros { ...human, ...stormtrooper, ...droid } }, " +
                "fragment human on Human { name, height, __typename }, " +
                "fragment stormtrooper on Stormtrooper { name, height, specialization, __typename }, " +
                "fragment droid on Droid { name, primaryFunction, __typename }");
            Test.DeepEquals(
                results,
                "{ heros: [ " +
                "{ name: 'Han Solo', height: 5.6430448,  __typename: 'Human' }, " +
                "{ name: 'FN-2187', height: 4.9, specialization: 'Imperial Snowtrooper', __typename: 'Stormtrooper' }, " +
                "{ name: 'R2-D2', primaryFunction: 'Astromech', __typename: 'Droid' } ] }"
                );
        }

        public static void InlineFragments<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery(
                "{ heros { __typename, ... on Droid { name }, ... on Human { name, height }, ... on Stormtrooper { name, height, specialization }, " +
                "... on Droid { primaryFunction } } }");
            Test.DeepEquals(
                results,
                "{ heros: [ " +
                "{ __typename: 'Human', name: 'Han Solo', height: 5.6430448 }, " +
                "{ __typename: 'Stormtrooper', name: 'FN-2187', height: 4.9, specialization: 'Imperial Snowtrooper' }, " +
                "{ __typename: 'Droid', name: 'R2-D2', primaryFunction: 'Astromech' } ] }"
                );
        }

        public static void InlineFragmentWithListField<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery(
                "{ heros { __typename, ... on Droid { name }, ... on Human { name, height, vehicles { name } }, ... on Stormtrooper { name, height, vehicles { name }, specialization } " +
                "... on Droid { primaryFunction } } }");
            Test.DeepEquals(
                results,
                "{ heros: [ " +
                "{ __typename: 'Human', name: 'Han Solo', height: 5.6430448, vehicles: [ { name: 'Millennium falcon' } ] }, " +
                "{ __typename: 'Stormtrooper', name: 'FN-2187', height: 4.9, vehicles: [ { name: 'Speeder bike' } ], specialization: 'Imperial Snowtrooper' }, " +
                "{ __typename: 'Droid', name: 'R2-D2', primaryFunction: 'Astromech' } ] }"
                );
        }

        public static void FragmentWithMultiLevelInheritance<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ heros { ... on ICharacter { name, __typename }, ... on Stormtrooper { height, specialization } } }");
            Test.DeepEquals(
                results,
                "{ heros: [ " +
                "{ name: 'Han Solo', __typename: 'Human'}, " +
                "{ name: 'FN-2187', __typename: 'Stormtrooper',  height: 4.9, specialization: 'Imperial Snowtrooper'}, " +
                "{ name: 'R2-D2', __typename: 'Droid' } ] }"
                );
        }

        public static void InlineFragmentWithoutTypenameField<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ heros { ... on Human { name }, ... on Droid { name }, ... on Stormtrooper { name, height, specialization } } }");
            Test.DeepEquals(
                results,
                "{ heros: [ " +
                "{ name: 'Han Solo'}, " +
                "{ name: 'FN-2187', height: 4.9, specialization: 'Imperial Snowtrooper'}, " +
                "{ name: 'R2-D2' } ] }"
                );
        }

        public static void InlineFragmentWithoutTypenameFieldWithoutOtherFields<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("{ heros { ... on Stormtrooper { height, specialization } } }");
            Test.DeepEquals(
                results,
                "{ heros: [ " +
                "{ }, " +
                "{ height: 4.9, specialization: 'Imperial Snowtrooper'}, " +
                "{ } ] }"
                );
        }

        public static void FragmentWithoutTypenameField<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery(
                "{ heros { ...human, ...droid, ...stormtrooper } }, fragment human on Human { name }, fragment droid on Droid { name }, fragment stormtrooper on Stormtrooper { name, height, specialization } ");
            Test.DeepEquals(
                results,
                "{ heros: [ " +
                "{ name: 'Han Solo',}, " +
                "{ name: 'FN-2187', height: 4.9, specialization: 'Imperial Snowtrooper'}, " +
                "{ name: 'R2-D2', } ] }"
                );
        }

        public static void FragmentWithMultipleTypenameFields<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery(
                "{ heros { ...human, ...droid, ...stormtrooper, __typename } }, fragment human on Human { name }, fragment droid on Droid { name }, fragment stormtrooper on Stormtrooper { name, height, specialization, __typename } ");
            Test.DeepEquals(
                results,
                "{ heros: [ " +
                "{ name: 'Han Solo', __typename: 'Human'}, " +
                "{ name: 'FN-2187', height: 4.9, specialization: 'Imperial Snowtrooper', __typename: 'Stormtrooper'}, " +
                "{ name: 'R2-D2', __typename: 'Droid'} ] }"
                );
        }

        public static void FragmentWithMultipleTypenameFieldsMixedWithInlineFragment<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery(
                "{ heros { ...stormtrooper, ... on Human {name}, ... on Droid {name}, __typename}}, fragment stormtrooper on Stormtrooper { name, height, specialization, __typename } ");
            Test.DeepEquals(
                results,
                "{ heros: [ " +
                "{ name: 'Han Solo', __typename: 'Human' }, " +
                "{ name: 'FN-2187', height: 4.9, specialization: 'Imperial Snowtrooper', __typename: 'Stormtrooper' }, " +
                "{ name: 'R2-D2', __typename: 'Droid' } ] }"
                );
        }
    }
}

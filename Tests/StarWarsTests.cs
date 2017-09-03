using GraphQL.Net;

namespace Tests
{
    public class StarWarsTests
    {
        public static void BasicQueryHero<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("query HeroNameQuery { hero { name } }");
            Test.DeepEquals(results, "{ hero: { name: 'R2-D2' } }");
        }

        public static void BasicQueryHeroWithIdAndFriends<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("query HeroNameQuery { hero { id, name, friends { name } } }");
            Test.DeepEquals(results,
                "{ hero: { id: '2001', name: 'R2-D2', friends: [ { name: 'Luke Skywalker' },{ name: 'Han Solo' }, { name: 'Leia Organa'} ] } }");
        }
        
        public static void BasicQueryHeroWithIdAndFriendsOfFriends<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("query HeroNameQuery { hero { id, name, friends { name, appearsIn, friends { name } } } }");
            Test.DeepEquals(results,
                "{ hero: { id: '2001', name: 'R2-D2', friends: [ { name: 'Luke Skywalker' },{ name: 'Han Solo' }, { name: 'Leia Organa'} ] } }");
        }
    }
}
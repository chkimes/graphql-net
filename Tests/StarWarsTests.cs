﻿using GraphQL.Net;

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

        public static void BasicQueryHeroWithFriendsOfFriends<TContext>(GraphQL<TContext> gql)
        {
            var results =
                gql.ExecuteQuery(
                    "query HeroNameQuery { hero { name, friends { name, appearsIn, friends { name } } } }");
            Test.DeepEquals(results,
                @"{hero: {
                  name: 'R2-D2',
                  friends: [
                    {
                      name: 'Luke Skywalker',
                      appearsIn: [ 'NEWHOPE', 'EMPIRE', 'JEDI' ],
                      friends: [
                        {
                          name: 'Han Solo',
                        },
                        {
                          name: 'Leia Organa',
                        },
                        {
                          name: 'C-3PO',
                        },
                        {
                          name: 'R2-D2',
                        }
                      ]
                    },
                    {
                      name: 'Han Solo',
                      appearsIn: [ 'NEWHOPE', 'EMPIRE', 'JEDI' ],
                      friends: [
                        {
                          name: 'Luke Skywalker',
                        },
                        {
                          name: 'Leia Organa',
                        },
                        {
                          name: 'R2-D2',
                        }
                      ]
                    },
                    {
                      name: 'Leia Organa',
                      appearsIn: [ 'NEWHOPE', 'EMPIRE', 'JEDI' ],
                      friends: [
                        {
                          name: 'Luke Skywalker',
                        },
                        {
                          name: 'Han Solo',
                        },
                        {
                          name: 'C-3PO',
                        },
                        {
                          name: 'R2-D2',
                        }
                      ]
                    }
                  ]
                }
              }");
        }

        public static void BasicQueryFetchLuke<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("query FetchLukeQuery { human(id: \"1000\") { name } }");
            Test.DeepEquals(results,
                "{ human: { name: 'Luke Skywalker' } }");
        }

        public static void FragmentsDuplicatedContent<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery(
                @"
            query DuplicateFields {
              luke: human(id: ""1000"") {
                name
                  homePlanet
              }
              leia: human(id: ""1003"") {
                name
                  homePlanet
              }
            }
            "
            );
            Test.DeepEquals(results,
                @"
                {
                  luke: {
                    name: 'Luke Skywalker',
                    homePlanet: 'Tatooine'
                  },
                  leia: {
                    name: 'Leia Organa',
                    homePlanet: 'Alderaan'
                  }
                }
            ");
        }

        public static void FragmentsAvoidDuplicatedContent<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery(
                @"query UseFragment {
                  luke: human(id: ""1000"") {
                      ...HumanFragment
                  }
                  leia: human(id: ""1003"") {
                    ...HumanFragment
                  }
                }
                fragment HumanFragment on Human {
                  name
                  homePlanet
                }
            "
            );
            Test.DeepEquals(results,
                @"
                {
                  luke: {
                    name: 'Luke Skywalker',
                    homePlanet: 'Tatooine'
                  },
                  leia: {
                    name: 'Leia Organa',
                    homePlanet: 'Alderaan'
                  }
                }
            ");
        }

        public static void FragmentsInlineFragments<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery(
                @"query UseInlineFragments {
                  luke: hero(episode: EMPIRE) {
                      ...HumanFragment,
                      ...DroidFragment
                  }
                  r2d2: hero {
                    ...HumanFragment,
                    ...DroidFragment
                  }
                }
                fragment HumanFragment on Human {
                  name
                  homePlanet
                }
                fragment DroidFragment on Droid {
                  name
                  primaryFunction
                }
            "
            );
            Test.DeepEquals(results,
                @"
                {
                  luke: {
                    name: 'Luke Skywalker',
                    homePlanet: 'Tatooine'
                  },
                  r2d2: {
                    name: 'R2-D2',
                    primaryFunction: 'Astromech'
                  }
                }
            ");
        }

        public static void TypenameR2Droid<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("query CheckTypeOfR2 { hero { __typename, name } }");
            Test.DeepEquals(results,
                "{ hero: { __typename: 'Droid', name: 'R2-D2' } }");
        }

        public static void TypenameLukeHuman<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery("query CheckTypeOfLuke { hero(episode: EMPIRE) { __typename, name } }");
            Test.DeepEquals(results,
                "{ hero: { __typename: 'Human', name: 'Luke Skywalker' } }");
        }

        public static void IntrospectionDroidType<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery(
                @"query IntrospectionDroidTypeQuery {
                    __type(name: ""Droid"") {
                      name
                    }
                  }");
            Test.DeepEquals(results,
                "{ __type: { name: 'Droid' } }");
        }

        public static void IntrospectionDroidTypeKind<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery(
                @"query IntrospectionDroidKindQuery {
                    __type(name: ""Droid"") {
                      name
                      kind
                    }
                  }");
            Test.DeepEquals(results,
                "{ __type: { name: 'Droid', kind: 'OBJECT' } }");
        }

        public static void IntrospectionCharacterInterface<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery(
                @"query IntrospectionCharacterKindQuery {
                  __type(name: ""ICharacter"") {
                    name
                    kind
                  }
                }");
            Test.DeepEquals(results,
                "{ __type: { name: 'ICharacter', kind: 'INTERFACE' } }");
        }

        public static void UnionTypeStarship<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery(
                @"query UnionTypeStarshipQuery {
                  search(text: ""starship"") {
                    __typename
                  }
                }");
            Test.DeepEquals(results,
                "{ search: { __typename: 'Starship'} }");
        }
      
        public static void UnionTypeHuman<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery(
                @"query UnionTypeStarshipQuery {
                  search(text: ""human"") {
                    __typename
                  }
                }");
            Test.DeepEquals(results,
                "{ search: { __typename: 'Human'} }");
        }
      
        public static void UnionTypeDroid<TContext>(GraphQL<TContext> gql)
        {
            var results = gql.ExecuteQuery(
                @"query UnionTypeStarshipQuery {
                  search(text: ""droid"") {
                    __typename
                  }
                }");
            Test.DeepEquals(results,
                "{ search: { __typename: 'Droid'} }");
        }
    }
}
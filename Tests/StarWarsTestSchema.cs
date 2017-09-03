using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Net;

namespace Tests
{
    public class StarWarsTestSchema
    {
        /*
         * The star wars schema is based on:
         * https://github.com/graphql/graphql-js/blob/master/src/__tests__/starWarsSchema.js
         *
         * Using our shorthand to describe type systems, the type system for our
         * Star Wars example is:
         *
         * enum Episode { NEWHOPE, EMPIRE, JEDI }
         *
         * interface Character {
         *   id: String!
         *   name: String
         *   friends: [Character]
         *   appearsIn: [Episode]
         * }
         *
         * type Human implements Character {
         *   id: String!
         *   name: String
         *   friends: [Character]
         *   appearsIn: [Episode]
         *   homePlanet: String
         * }
         *
         * type Droid implements Character {
         *   id: String!
         *   name: String
         *   friends: [Character]
         *   appearsIn: [Episode]
         *   primaryFunction: String
         * }
         *
         * type Query {
         *   hero(episode: Episode): Character
         *   human(id: String!): Human
         *   droid(id: String!): Droid
         * }
         */

        public enum EpisodeEnum
        {
            NEWHOPE = 4,
            EMPIRE = 5,
            JEDI = 6
        }

        public interface ICharacter
        {
            string Id { get; set; }
            string Name { get; set; }
            ICollection<ICharacter> Friends { get; set; }
            ICollection<EpisodeEnum> AppearsIn { get; set; }
            string SecretBackstory { get; set; }
        }

        public class Human : ICharacter
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public ICollection<ICharacter> Friends { get; set; }
            public ICollection<EpisodeEnum> AppearsIn { get; set; }
            public string SecretBackstory { get; set; }
            public string HomePlanet { get; set; }
        }

        public class Droid : ICharacter
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public ICollection<ICharacter> Friends { get; set; }
            public ICollection<EpisodeEnum> AppearsIn { get; set; }
            public string SecretBackstory { get; set; }
            public string PrimaryFunction { get; set; }
        }

        public static void Create<TContext>(GraphQLSchema<TContext> schema,
            Func<TContext, IQueryable<ICharacter>> herosProviderFunc)
        {
            schema.AddEnum<EpisodeEnum>();
            
            var characterInterface = schema.AddInterfaceType<ICharacter>();
            characterInterface.AddAllFields();

            var humanType = schema.AddType<Human>();
            humanType.AddAllFields();
            humanType.AddInterface(characterInterface);

            var droidType = schema.AddType<Droid>();
            droidType.AddAllFields();
            droidType.AddInterface(characterInterface);

            schema.AddField(
                "hero",
                new {episode = EpisodeEnum.NEWHOPE},
                (db, args) => args.episode == EpisodeEnum.EMPIRE
                    // Luke is the hero of Episode V.
                    ? herosProviderFunc(db).FirstOrDefault(h => h.Id == "1000")
                    // Artoo is the hero otherwise.
                    : herosProviderFunc(db).FirstOrDefault(h => h.Id == "2001"));
            schema.AddField("human", new {id = ""},
                (db, args) => herosProviderFunc(db).OfType<Human>().FirstOrDefault(c => c.Id == args.id));
            schema.AddField("droid", new {id = ""},
                (db, args) => herosProviderFunc(db).OfType<Droid>().FirstOrDefault(c => c.Id == args.id));
        }


        public static ICollection<ICharacter> CreateData()
        {
            var luke = new Human
            {
                Id = "1000",
                Name = "Luke Skywalker",
                AppearsIn = new List<EpisodeEnum>
                {
                    EpisodeEnum.NEWHOPE,
                    EpisodeEnum.EMPIRE,
                    EpisodeEnum.JEDI
                },
                HomePlanet = "Tatooine"
            };
            var vader = new Human
            {
                Id = "1001",
                Name = "Darth Vader",
                AppearsIn = new List<EpisodeEnum>
                {
                    EpisodeEnum.NEWHOPE,
                    EpisodeEnum.EMPIRE,
                    EpisodeEnum.JEDI
                },
                HomePlanet = "Tatooine"
            };
            var han = new Human
            {
                Id = "1002",
                Name = "Han Solo",
                AppearsIn = new List<EpisodeEnum>
                {
                    EpisodeEnum.NEWHOPE,
                    EpisodeEnum.EMPIRE,
                    EpisodeEnum.JEDI
                }
            };
            var leia = new Human
            {
                Id = "1003",
                Name = "Leia Organa",
                AppearsIn = new List<EpisodeEnum>
                {
                    EpisodeEnum.NEWHOPE,
                    EpisodeEnum.EMPIRE,
                    EpisodeEnum.JEDI
                },
                HomePlanet = "Alderaan"
            };
            var tarkin = new Human
            {
                Id = "1004",
                Name = "Wilhuff Tarkin",
                AppearsIn = new List<EpisodeEnum>
                {
                    EpisodeEnum.NEWHOPE
                }
            };
            var threepio = new Droid
            {
                Id = "2000",
                Name = "C-3PO",
                AppearsIn = new List<EpisodeEnum>
                {
                    EpisodeEnum.NEWHOPE,
                    EpisodeEnum.EMPIRE,
                    EpisodeEnum.JEDI
                },
                PrimaryFunction = "Protocol"
            };
            var artoo = new Droid
            {
                Id = "2001",
                Name = "R2-D2",
                AppearsIn = new List<EpisodeEnum>
                {
                    EpisodeEnum.NEWHOPE,
                    EpisodeEnum.EMPIRE,
                    EpisodeEnum.JEDI
                },
                PrimaryFunction = "Astromech"
            };
            luke.Friends = new List<ICharacter>
            {
                han,
                leia,
                threepio,
                artoo
            };
            vader.Friends = new List<ICharacter>
            {
                tarkin
            };
            han.Friends = new List<ICharacter>
            {
                luke,
                leia,
                artoo
            };
            leia.Friends = new List<ICharacter>
            {
                luke,
                han,
                threepio,
                artoo
            };
            tarkin.Friends = new List<ICharacter>
            {
                vader
            };
            threepio.Friends = new List<ICharacter>
            {
                luke,
                han,
                leia,
                artoo
            };
            artoo.Friends = new List<ICharacter>
            {
                luke,
                han,
                leia
            };

            return new List<ICharacter>
            {
                luke,
                vader,
                han,
                leia,
                tarkin,
                threepio,
                artoo
            };
        }
    }
}
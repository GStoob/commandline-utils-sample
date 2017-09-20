using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Samples.CommandLineUtils
{
    static class Program
    {
        const string GetByIdFlag = "-i |--id";
        const string SearchForCharacterFlag = "-s |--search";
        const string HelpFlag = "-? |-h |--help";
        const string ApiBaseUrl = "https://swapi.co/api";

        static int Main(string[] args)
        {
            try
            {
                return Run(args);
            }
            catch (Exception e)
            {
                var message = e.GetBaseException().Message;
                Console.Error.WriteLine(message);
                return 0xbad;
            }
        }

        private static int Run(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false);

            app.HelpOption(HelpFlag);

            app.Command("characters", c =>
            {
                c.Description = "With this command, you can retrieve information about characters from Star Wars using the Star Wars Web API.";

                var GetByIdOption = c.Option(GetByIdFlag, "Get a specific character by ID.", CommandOptionType.SingleValue);
                var searchForCharacterOption = c.Option(SearchForCharacterFlag, "Search a character in the star wars API. Use this option if you don't know the unique ID of a specific character.", CommandOptionType.SingleValue);
                
                c.OnExecute(async () =>
                {
                    if (GetByIdOption.HasValue())
                    {
                        var character = await GetStarWarsCharacterById(GetByIdOption.Value());        
                        DisplayStarWarsCharacter(character);
                        return 0;
                    }
                    else if (searchForCharacterOption.HasValue())
                    {
                        var characters = await SearchStarWarsCharacter(searchForCharacterOption.Value());

                        foreach (var character in characters)
                        {
                            DisplayStarWarsCharacter(character);
                            Console.WriteLine();
                        }
                        return 0;
                    }
                    else
                    {
                        c.ShowHelp();
                        return 0;
                    }
                });
            });

            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 0;
            });
            return app.Execute(args);
        }

        private static async Task<StarWarsCharacter> GetStarWarsCharacterById(string characterId)
        {
            using (var client = new HttpClient())
            {
                var request = await client.GetAsync($"{ApiBaseUrl}/people/{characterId}");

                request.EnsureSuccessStatusCode();

                var result = await request.Content.ReadAsStringAsync();

                var deserializedCharacter = JsonConvert.DeserializeObject<StarWarsCharacter>(result);

                if (deserializedCharacter == null) throw new Exception("An error occured while deserializing the result received from the Star Wars API!");

                return deserializedCharacter;
            }
        }

        private static async Task<IEnumerable<StarWarsCharacter>> SearchStarWarsCharacter(string searchTerm)
        {
            using (var client = new HttpClient())
            {
                var request = await client.GetAsync($"{ApiBaseUrl}/people/?search={searchTerm}");

                request.EnsureSuccessStatusCode();

                var result = await request.Content.ReadAsStringAsync();

                // We are just interested in the search results, so we have to extract the specific node ('results') from the rest of this json.
                var jObject = JObject.Parse(result);
                var jToken = jObject.GetValue("results");
                var characters = (List<StarWarsCharacter>)jToken.ToObject(typeof(List<StarWarsCharacter>));

                if (!characters.Any()) throw new Exception("An error occured while deserializing the result received from the Star Wars API!");

                return characters;
            }
        }

        private static void DisplayStarWarsCharacter(StarWarsCharacter character)
        {
            Console.WriteLine($"{nameof(character.Name)}: {character.Name}");
            Console.WriteLine($"Birth year: {character.BirthYear}");
            Console.WriteLine($"{nameof(character.Height)}: {character.Height}");
            Console.WriteLine($"Eye color: {character.EyeColor}");
        }
    }
}
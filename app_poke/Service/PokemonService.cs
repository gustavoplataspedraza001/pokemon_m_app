using app_poke.Models.Pokemon;

namespace app_poke.Service
{
    public class PokemonApiService
    {
        private readonly HttpClient _httpClient;

        public PokemonApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<string>> GetAllTypesAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<TypeResponse>("https://pokeapi.co/api/v2/type");
            return response?.Results.Select(t => t.Name).ToList() ?? new List<string>();
        }

        public async Task<List<Pokemon>> GetPokemonsAsync(int offset = 0, int limit = 20)
        {
            var speciesResponse = await _httpClient.GetFromJsonAsync<PokemonSpeciesResponse>($"https://pokeapi.co/api/v2/pokemon-species?offset={offset}&limit={limit}");
            var pokemons = new List<Pokemon>();

            if (speciesResponse?.Results == null)
                return pokemons;

            foreach (var species in speciesResponse.Results)
            {
                var details = await _httpClient.GetFromJsonAsync<PokemonDetails>($"https://pokeapi.co/api/v2/pokemon/{species.Name}");

                pokemons.Add(new Pokemon
                {
                    Name = species.Name,
                    ImageUrl = details?.Sprites?.FrontDefault ?? "",
                    Types = details?.Types?.Select(t => t.Type.Name).ToList() ?? new List<string>()
                });
            }

            return pokemons;
        }

    }
}

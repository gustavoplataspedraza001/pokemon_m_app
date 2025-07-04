namespace app_poke.Models.Pokemon
{

    public class PokemonListViewModel
    {
        public List<Pokemon> Pokemons { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public List<string> PokemonTypes { get; set; } // para el combo
        public string SelectedType { get; set; }
        public string SearchName { get; set; }
    }
    public class Pokemon
    {
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public List<string> Types { get; set; } = new List<string>();

    }
    public class TypeResponse
    {
        public List<TypeResult> Results { get; set; }
    }
    public class TypeResult
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }
    public class PokemonSpeciesResponse
    {
        public int Count { get; set; }
        public string Next { get; set; }
        public string Previous { get; set; }
        public List<PokemonSpeciesResult> Results { get; set; }
    }
    public class PokemonSpeciesResult
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }
    public class PokemonDetails
    {
        public Sprites Sprites { get; set; }
        public List<TypeSlot> Types { get; set; }
    }
    public class Sprites
    {
        public string FrontDefault { get; set; }
    }
    public class TypeSlot
    {
        public TypeInfo Type { get; set; }
    }
    public class TypeInfo
    {
        public string Name { get; set; }
    }
}

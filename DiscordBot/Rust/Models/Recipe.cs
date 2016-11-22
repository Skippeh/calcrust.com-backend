namespace DiscordBot.Rust.Models
{
    public class Recipe
    {
        public class ItemCount
        {
            public float Count;
            public Item Item;
        }

        public ItemCount[] Input;
        public ItemCount Output;
        public int TTC;
    }
}
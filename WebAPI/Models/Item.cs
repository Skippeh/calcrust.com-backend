namespace WebAPI.Models
{
    public class Item
    {
        public string Shortname { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MaxStack { get; set; }
        public string Category { get; set; }
        public ItemMeta Meta { get; set; }

        public override string ToString()
        {
            return $"[Item] {Name}";
        }

        public Recipe GetRecipe()
        {
            if (DataManager.Data.Recipes.ContainsKey(Shortname))
                return DataManager.Data.Recipes[Shortname];

            return null;
        }
    }
}
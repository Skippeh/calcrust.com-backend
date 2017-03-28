using System.Collections.Generic;
using System.Linq;
using RustCalc.Common.Models;

namespace RustCalc.Api.V1.Models
{
    public class ExtendedItemV1
    {
        public string Name => item.Name;
        public string Shortname => item.Shortname;
        public ItemCategory Category => item.Category;
        public int StackSize => item.StackSize;
        public int ItemId => item.ItemId;

        public readonly List<string> Tags = new List<string>(); 

        private readonly ExportData data;
        private readonly Item item;

        public ExtendedItemV1(Item item, ExportData data)
        {
            this.item = item;
            this.data = data;

            if (data.Recipes.Any(recipe => recipe.Key == item))
            {
                Tags.Add("recipe");
            }

            if (data.Destructibles.Any(kv => kv.Key == item.Shortname))
            {
                Tags.Add("destructible");
            }

            if (data.Recycler.Any(kv => kv.Key == item))
            {
                Tags.Add("recyclable");
            }

            if (SkinsManager.Skins.Any(kv => kv.Key == item))
            {
                Tags.Add("skinnable");
            }
        }
    }
}
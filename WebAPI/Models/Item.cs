﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebAPI.Models
{
    public class Item
    {
        public string Shortname { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MaxStack { get; set; }
        public string Category { get; set; }
        public Dictionary<MetaType, ItemMeta> Meta { get; set; }
        public List<string> Descriptions { get; set; } = new List<string>();
        
        public override string ToString()
        {
            return $"[Item] {Name}";
        }

        public Recipe GetRecipe(RustData rustData)
        {
            if (rustData.Recipes.ContainsKey(Shortname))
                return rustData.Recipes[Shortname];

            return null;
        }
    }
}
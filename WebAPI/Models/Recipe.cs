using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace WebAPI.Models
{
    public class Recipe
    {
        public class Item
        {
            public Models.Item Result { get; set; }
            public double Count { get; set; }

            public Item(double count, Models.Item item)
            {
                Count = count;
                Result = item;
            }
        }

        public class Requirements
        {
            public bool TotalRequirements { get; set; }
            public double Count { get; set; }
            public double TTC { get; set; }

            public List<Item> Input { get; private set; } = new List<Item>();
            public Item Output { get; set; }
        }

        [JsonIgnore]
        public virtual List<Item> Input { get; set; } = new List<Item>();

        [JsonIgnore]
        public virtual Item Output { get; set; }

        public int TTC { get; set; }
        public string Rarity { get; set; }
        public bool Researchable { get; set; }

        public override string ToString()
        {
            return $"[Recipe] {Output.Result.Name} ({Output.Count}x)";
        }

        public Requirements CalculateRequirements(double amount, bool totalRequirements, bool ceilValues)
        {
            double outputCount = Output.Count * amount;
            var requirements = new Requirements
            {
                TotalRequirements = totalRequirements,
                Count = amount,
                Output = new Item(outputCount, Output.Result)
            };

            var neededItems = totalRequirements ? CalculateNeededItems() : Input.Select(item => new Item(item.Count, item.Result)).ToArray();

            foreach (var item in neededItems)
            {
                item.Count *= amount;

                if (ceilValues)
                    item.Count = Math.Ceiling(item.Count);
            }

            requirements.Input.AddRange(neededItems);
            requirements.TTC = totalRequirements ? CalculateTotalTTC(neededItems, amount) : TTC * amount;

            if (ceilValues)
                requirements.TTC = Math.Round(requirements.TTC);

            return requirements;
        }

        public Item[] CalculateNeededItems()
        {
            var result = new Dictionary<string, Item>();
            
            Action<Item, double> addItem = (Item item, double amount) =>
            {
                if (result.ContainsKey(item.Result.Shortname))
                {
                    result[item.Result.Shortname].Count += item.Count * amount;
                }
                else
                {
                    result.Add(item.Result.Shortname, new Item(item.Count * amount, item.Result));
                }
            };

            foreach (Item item in Input)
            {
                Recipe recipe = item.Result.GetRecipe();

                if (recipe != null)
                {
                    var craftsNeeded = item.Count / recipe.Output.Count;
                    var requirements = recipe.CalculateNeededItems();

                    foreach (var neededItem in requirements)
                    {
                        addItem(neededItem, craftsNeeded);
                    }

                    addItem(item, 1);
                }
                else
                {
                    addItem(item, 1);
                }
            }

            return result.Values.ToArray();
        }

        private double CalculateTotalTTC(Item[] items, double count)
        {
            double ttc = TTC * count;

            foreach (var recipeItem in items)
            {
                Recipe recipe = recipeItem.Result.GetRecipe();

                if (recipe != null)
                {
                    double divide = recipe.Output.Count;

                    ttc += (recipeItem.Count / divide) * recipe.TTC;
                }
            }

            return ttc;
        }
    }
}
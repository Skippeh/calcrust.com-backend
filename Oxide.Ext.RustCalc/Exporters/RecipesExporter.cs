﻿using System.Linq;
using RustCalc.Common;
using RustCalc.Common.Exporting;
using RustCalc.Common.Models;
using RustCalc.Common.Serializing;

namespace RustCalc.Exporters
{
    [Exporter(typeof(ItemsExporter))]
    public class RecipesExporter : IExporter
    {
        public string ID => "Recipes";
        public object ExportData(ExportData data)
        {
            var recipes = new SerializableList<Recipe>();

            foreach (var itemRecipe in ItemManager.bpList)
            {
                if (!itemRecipe.userCraftable || !itemRecipe.enabled)
                    continue;

                recipes.Add(new Recipe
                {
                    TimeToCraft = itemRecipe.time,
                    Input = itemRecipe.ingredients.Select(itemAmount => new Common.Models.ItemAmount
                    {
                        Item = data.Items.First(item => item.ItemId == itemAmount.itemid),
                        Amount = itemAmount.amount
                    }).ToSerializableList(),
                    Output = new Common.Models.ItemAmount
                    {
                        Item = data.Items.First(item => item.ItemId == itemRecipe.targetItem.itemid),
                        Amount = itemRecipe.amountToCreate
                    }
                });
            }

            return recipes;
        }
    }
}
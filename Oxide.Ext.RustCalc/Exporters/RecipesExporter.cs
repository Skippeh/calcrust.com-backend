using System.Linq;
using RustCalc.Common;
using RustCalc.Common.Models;
using RustCalc.Common.Serializing;
using RustCalc.Exporting;
using ItemAmount = RustCalc.Common.Models.ItemAmount;

namespace RustCalc.Exporters
{
    [Exporter(typeof(ItemsExporter))]
    public class RecipesExporter : IExporter
    {
        public string ID => "recipes";
        public IBinarySerializable ExportData(ExportData data)
        {
            var recipes = new SerializableList<Recipe>(false);

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
                    }).ToSerializableList(false),
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
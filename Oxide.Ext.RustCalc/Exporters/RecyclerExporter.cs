using System.Collections.Generic;
using System.Linq;
using RustCalc.Common;
using RustCalc.Common.Exporting;
using RustCalc.Common.Models;

namespace RustCalc.Exporters
{
    [Exporter(typeof(ItemsExporter))]
    public class RecyclerExporter : IExporter
    {
        public string ID => "Recycler";

        public object ExportData(ExportData data)
        {
            var result = new Dictionary<Common.Models.Item, RecycleOutput>();

            foreach (Common.Models.Item item in data.Items.Values)
            {
                var blueprint = ItemManager.itemList.First(x => x.itemid == item.ItemId).Blueprint;

                if (blueprint == null)
                    continue;

                var recycleOutput = new RecycleOutput();
                recycleOutput.Output.AddRange(blueprint.ingredients.ToSerializableList(false, amount => new Common.Models.ItemAmount
                {
                    Amount = amount.amount,
                    Item = data.Items[amount.itemid]
                }));

                result.Add(item, recycleOutput);
            }

            return result;
        }
    }
}
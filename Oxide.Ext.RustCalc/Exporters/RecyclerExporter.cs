using System.Linq;
using RustCalc.Common;
using RustCalc.Common.Exporting;
using RustCalc.Common.Models;
using RustCalc.Common.Serializing;

namespace RustCalc.Exporters
{
    [Exporter(typeof(ItemsExporter))]
    public class RecyclerExporter : IExporter
    {
        public string ID => "recycler";

        public IBinarySerializable ExportData(ExportData data)
        {
            var result = new SerializableDictionary<int, RecycleOutput>();

            foreach (Common.Models.Item item in data.Items)
            {
                var blueprint = ItemManager.itemList.First(x => x.itemid == item.ItemId).Blueprint;

                if (blueprint == null)
                    continue;

                var recycleOutput = new RecycleOutput();
                recycleOutput.Output.AddRange(blueprint.ingredients.ToSerializableList(false, amount => new Common.Models.ItemAmount
                {
                    Amount = amount.amount,
                    Item = data.Items.First(x => x.ItemId == amount.itemid)
                }));

                result.Add(item.ItemId, recycleOutput);
            }

            return result;
        }
    }
}
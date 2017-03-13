using System.Linq;
using RustCalc.Common.Exporting;
using RustCalc.Common.Models;
using RustCalc.Common.Serializing;

namespace RustCalc.Exporters
{
    [Exporter]
    public class ItemsExporter : IExporter
    {
        public string ID => "items";
        public IBinarySerializable ExportData(ExportData data)
        {
            var list = new SerializableList<Common.Models.Item>();
            list.AddRange(ItemManager.itemList.Select(itemDefinition => new Common.Models.Item
            {
                Shortname = itemDefinition.shortname,
                Name = itemDefinition.displayName.english,
                Category = itemDefinition.category,
                StackSize = itemDefinition.stackable,
                ItemId = itemDefinition.itemid
            }));

            return list;
        }
    }
}
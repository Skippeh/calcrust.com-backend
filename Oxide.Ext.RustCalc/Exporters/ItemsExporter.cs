using System.Collections.Generic;
using System.Linq;
using RustCalc.Common.Exporting;
using RustCalc.Common.Models;
using RustCalc.Common.Serializing;

namespace RustCalc.Exporters
{
    [Exporter]
    public class ItemsExporter : IExporter
    {
        public string ID => "Items";
        public object ExportData(ExportData data)
        {
            var result = new Dictionary<int, Common.Models.Item>();

            IEnumerable<Common.Models.Item> items = ItemManager.itemList.Where(def => !Utility.ItemExcludeList.Contains(def.shortname)).Select(itemDefinition => new Common.Models.Item
            {
                Shortname = itemDefinition.shortname,
                Name = itemDefinition.displayName.english,
                Category = (Common.Models.ItemCategory) itemDefinition.category,
                StackSize = itemDefinition.stackable,
                ItemId = itemDefinition.itemid
            });

            foreach (Common.Models.Item item in items)
            {
                result.Add(item.ItemId, item);
            }

            return result;
        }
    }
}
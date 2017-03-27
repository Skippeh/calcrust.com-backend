using System.Collections.Generic;
using System.Linq;
using Nancy;
using RustCalc.Api.V1.Models;
using RustCalc.Common.Models;

namespace RustCalc.Api.V1.Modules
{
    public class ItemsModuleV1 : RustCalcModuleV1
    {
        public ItemsModuleV1() : base("/items")
        {
            Get["/"] = WrapMethod(_ => GetItems());
            Get["/{shortname}"] = WrapMethod((dynamic _) => GetItem(_.shortname));
        }

        private List<Item> GetItems()
        {
            return Data.Items.Values.ToList();
        }

        public ExtendedItemV1 GetItem(string shortname)
        {
            var lowerShortname = shortname.ToLower();
            var item = Data.Items.FirstOrDefault(kv => kv.Value.Shortname.ToLower() == lowerShortname).Value;

            if (item == null)
                throw new ApiResponseException(HttpStatusCode.NotFound, $"No item found with shortname '{shortname}'.");

            var extendedItem = new ExtendedItemV1(item, Data);
            return extendedItem;
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using Nancy;
using RustCalc.Common.Models;

namespace RustCalc.Api.V1.Modules
{
    public class SkinsModuleV1 : RustCalcModuleV1
    {
        public SkinsModuleV1() : base("/skins")
        {
            Get["/"] = WrapMethod(_ => GetSkins());
            Get["/{shortname}"] = WrapMethod((dynamic _) => GetItemSkins((string) _.shortname));
        }

        private Dictionary<string, List<ItemSkin>> GetSkins()
        {
            return SkinsManager.Skins.ToDictionary(kv => kv.Key.Shortname, kv => kv.Value);
        }

        private List<ItemSkin> GetItemSkins(string shortname)
        {
            string lowerShortname = shortname.ToLower();
            var skinList = SkinsManager.Skins.FirstOrDefault(kv => kv.Key.Shortname.ToLower() == lowerShortname).Value;

            if (skinList == null)
                throw new ApiResponseException(HttpStatusCode.NotFound, $"No item found with shortname '{shortname}'.");

            return skinList;
        }
    }
}
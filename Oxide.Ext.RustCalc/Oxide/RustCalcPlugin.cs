using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using RustCalc.Exporting;

namespace RustCalc.Oxide
{
    public class RustCalcPlugin : CSPlugin
    {
        [HookMethod("Init")]
        private void Init()
        {
            ExportManager.LoadExporters();
        }

        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            JObject data = ExportManager.ExportData();

            Interface.Oxide.LogInfo(JsonConvert.SerializeObject(data, Formatting.Indented));
        }
    }
}
using System.IO;
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
            using (var memstream = new MemoryStream())
            {
                var writer = new BinaryWriter(memstream);
                var data = ExportManager.ExportData();
                ExportManager.SerializeData(data, writer);

                Interface.Oxide.LogInfo("Serialized " + memstream.Length + " bytes of data");

                Interface.Oxide.LogInfo(Interface.Oxide.DataDirectory);

                using (var fileWriter = File.Create(Interface.Oxide.DataDirectory + "/rustcalc-export.bin"))
                {
                    byte[] bytes = new byte[memstream.Length];
                    memstream.Seek(0, SeekOrigin.Begin);
                    memstream.Read(bytes, 0, bytes.Length);
                    fileWriter.Write(bytes, 0, bytes.Length);
                }
            }
        }
    }
}
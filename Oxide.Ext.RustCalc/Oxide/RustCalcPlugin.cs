using System;
using System.Diagnostics;
using System.IO;
using Oxide.Core;
using Oxide.Core.Plugins;
using RustCalc.Common.Exporting;

namespace RustCalc.Oxide
{
    public class RustCalcPlugin : CSPlugin
    {
        private class OxideTraceListener : TraceListener
        {
            public override void Write(string message)
            {
                Interface.Oxide.LogInfo(message);
            }

            public override void WriteLine(string message)
            {
                Interface.Oxide.LogInfo(message);
            }
        }

        [HookMethod("Init")]
        private void Init()
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new OxideTraceListener());
            Trace.AutoFlush = true;

            ExportManager.LoadExporters();
        }

        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            try
            {
                using (var memstream = new MemoryStream())
                {
                    var writer = new BinaryWriter(memstream);

                    DateTime start = DateTime.UtcNow;

                    var data = ExportManager.ExportData();
                    ExportManager.SerializeData(data, writer);

                    DateTime end = DateTime.UtcNow;

                    data.Meta.ExportTime = end - start;

                    Interface.Oxide.LogInfo("Serialized " + memstream.Length + " bytes of data");

                    using (var fileWriter = File.Create(Interface.Oxide.DataDirectory + "/rustcalc-export.bin"))
                    {
                        byte[] bytes = new byte[memstream.Length];
                        memstream.Seek(0, SeekOrigin.Begin);
                        memstream.Read(bytes, 0, bytes.Length);
                        fileWriter.Write(bytes, 0, bytes.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogError(ex.ToString());
            }
        }
    }
}
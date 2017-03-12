using System.Collections.Generic;
using RustCalc.Common.Serializing;
using RustCalc.Exporting;

namespace RustCalc.Exporters
{
    [Exporter(typeof(ItemsExporter))]
    public class MetaExporter : IExporter
    {
        public string ID => "meta";
        public IBinarySerializable ExportData()
        {
            return null;
        }
    }
}
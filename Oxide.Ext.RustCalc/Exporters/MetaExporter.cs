using System.Collections.Generic;
using RustCalc.Common.Serializing;
using RustCalc.Exporting;

namespace RustCalc.Exporters
{
    [Exporter(typeof(ItemsExporter))]
    public class MetaExporter : IExporter
    {
        public string ID => "meta";
        public Dictionary<string, IBinarySerializer> ExportData()
        {
            var result = new Dictionary<string, IBinarySerializer>();



            return result;
        }
    }
}
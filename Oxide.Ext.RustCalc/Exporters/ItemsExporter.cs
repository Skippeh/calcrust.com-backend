using System.Collections.Generic;
using System.IO;
using RustCalc.Common.Models;
using RustCalc.Common.Serializing;
using RustCalc.Exporting;

namespace RustCalc.Exporters
{
    [Exporter]
    public class ItemsExporter : IExporter
    {
        public string ID => "items";
        public Dictionary<string, IBinarySerializable> ExportData()
        {
            var result = new Dictionary<string, IBinarySerializable>();



            return result;
        }
    }
}
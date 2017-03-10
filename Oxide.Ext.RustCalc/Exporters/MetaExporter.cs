using System;
using RustCalc.Exporting;

namespace RustCalc.Exporters
{
    [Exporter(typeof(ItemsExporter))]
    public class MetaExporter : IExporter
    {
        public string ID => "meta";

        public object ExportData()
        {
            return null;
        }
    }
}
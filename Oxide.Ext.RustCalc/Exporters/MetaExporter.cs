using System;
using RustCalc.Common.Exporting;
using RustCalc.Common.Models;
using RustCalc.Common.Serializing;

namespace RustCalc.Exporters
{
    [Exporter(typeof(ItemsExporter), typeof(RecipesExporter))]
    public class MetaExporter : IExporter
    {
        public string ID => "Meta";
        public object ExportData(ExportData data)
        {
            var meta = new Meta
            {
                Time = DateTime.UtcNow
            };
            
            return meta;
        }
    }
}
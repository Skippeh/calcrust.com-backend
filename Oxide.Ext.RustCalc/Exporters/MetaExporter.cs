using RustCalc.Common.Models;
using RustCalc.Common.Serializing;
using RustCalc.Exporting;

namespace RustCalc.Exporters
{
    [Exporter(typeof(ItemsExporter), typeof(RecipesExporter))]
    public class MetaExporter : IExporter
    {
        public string ID => "meta";
        public IBinarySerializable ExportData(ExportData data)
        {
            var meta = new Meta();



            return meta;
        }
    }
}
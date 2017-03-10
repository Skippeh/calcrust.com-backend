using RustCalc.Exporting;

namespace RustCalc.Exporters
{
    [Exporter]
    public class ItemsExporter : IExporter
    {
        public string ID => "items";

        public object ExportData()
        {
            return null;
        }
    }
}
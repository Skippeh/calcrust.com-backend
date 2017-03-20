using RustCalc.Common.Models;

namespace RustCalc.Common.Exporting
{
    public interface IExporter
    {
        string ID { get; }
        object ExportData(ExportData data);
    }
}
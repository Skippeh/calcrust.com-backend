using RustCalc.Common.Models;
using RustCalc.Common.Serializing;

namespace RustCalc.Common.Exporting
{
    public interface IExporter
    {
        string ID { get; }
        IBinarySerializable ExportData(ExportData data);
    }
}
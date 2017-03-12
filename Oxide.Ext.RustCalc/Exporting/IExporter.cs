using System.Collections.Generic;
using System.IO;
using RustCalc.Common.Serializing;

namespace RustCalc.Exporting
{
    public interface IExporter
    {
        string ID { get; }
        IBinarySerializable ExportData();
    }
}
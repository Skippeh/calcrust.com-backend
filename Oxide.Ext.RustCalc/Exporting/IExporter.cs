namespace RustCalc.Exporting
{
    public interface IExporter
    {
        string ID { get; }
        object ExportData();
    }
}
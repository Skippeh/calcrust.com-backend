using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using RustCalc.Common.Models;
using RustCalc.Common.Serializing;

namespace RustCalc.Common.Exporting
{
    public static class ExportManager
    {
        public static List<IExporter> Exporters { get; private set; } = new List<IExporter>();

        public static bool LoadExporters()
        {
            var attrDictionary = new Dictionary<Type, ExporterAttribute>();
            var exportTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.GetInterfaces().Contains(typeof(IExporter))).ToList();
            
            // Collect all exporters with their attribute data into exportTuples.
            foreach (Type exportType in exportTypes)
            {
                ExporterAttribute attribute = (ExporterAttribute) exportType.GetCustomAttributes(typeof(ExporterAttribute), true).FirstOrDefault();

                if (attribute == null)
                {
                    Trace.TraceError("Exporter missing ExporterAttribute: " + exportType.FullName);
                    return false;
                }

                attrDictionary.Add(exportType, attribute);
            }

            Queue<Type> queue = new Queue<Type>(attrDictionary.Keys);
            List<Type> loaded = new List<Type>();

            while (queue.Count > 0)
            {
                var exporterType = queue.Peek();
                var attributeData = attrDictionary[exporterType];

                if (attributeData.Dependencies.Count == 0 || attributeData.Dependencies.All(type => loaded.Contains(type)))
                {
                    exporterType = queue.Dequeue();
                    loaded.Add(exporterType);
                    var exporter = (IExporter) Activator.CreateInstance(exporterType);
                    Exporters.Add(exporter);
                }
                else
                {
                    var circularDependencies = attributeData.Dependencies.Where(type => attrDictionary[type].Dependencies.Contains(exporterType)).ToList();

                    if (circularDependencies.Count > 0)
                    {
                        Trace.TraceError("Circular exporter dependency detected between " + exporterType.FullName + " and:\n\t- {0}", String.Join("\n\t- ", circularDependencies.Select(type => type.FullName).ToArray()));
                        return false;
                    }

                    queue.Enqueue(queue.Dequeue()); // Put exporter at the back of the queue
                }
            }

            return true;
        }

        public static ExportData ExportData()
        {
            var result = new ExportData();
            Models.ExportData.SetCurrent(result);

            foreach (var exporter in Exporters)
            {
                var exportData = exporter.ExportData(result);

                if (exportData != null)
                {
                    try
                    {
                        var propertyInfo = typeof (ExportData).GetProperty(exporter.ID, BindingFlags.Instance | BindingFlags.Public);
                        propertyInfo.SetValue(result, exportData, null);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Failed to set exported data for " + exporter.ID + ": " + ex.Message);
                    }
                }
            }

            Trace.TraceInformation($"Exporting {result.Items.Count} items, and {result.Recipes.Count} recipes.");

            Models.ExportData.SetCurrent(null);
            return result;
        }

        public static void SerializeData(ExportData data, BinaryWriter writer)
        {
            Models.ExportData.SetCurrent(data);
            writer.Write(data);
            Models.ExportData.SetCurrent(null);
        }

        public static ExportData DeserializeData(BinaryReader reader)
        {
            var result = new ExportData();
            Models.ExportData.SetCurrent(result);
            result.Deserialize(reader);
            Models.ExportData.SetCurrent(null);
            return result;
        }
    }
}
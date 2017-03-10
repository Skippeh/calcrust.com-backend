using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;

namespace RustCalc.Exporting
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
                    Interface.Oxide.LogError("Exporter missing ExporterAttribute: " + exportType.FullName);
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
                        Interface.Oxide.LogError("Circular exporter dependency detected between " + exporterType.FullName + " and:\n\t- {0}", String.Join("\n\t- ", circularDependencies.Select(type => type.FullName).ToArray()));
                        return false;
                    }

                    queue.Enqueue(queue.Dequeue()); // Put exporter at the back of the queue
                }
            }

            Interface.Oxide.LogInfo("Exporters loaded:");
            foreach (var exporter in Exporters)
            {
                Interface.Oxide.LogInfo(" - " + exporter.GetType().Name);
            }

            return true;
        }

        public static JObject ExportData()
        {
            var result = new JObject();

            foreach (var exporter in Exporters)
            {
                object exportData = exporter.ExportData();

                if (exportData != null)
                    result.Add(exporter.ID, JToken.FromObject(exportData));
                else
                    result.Add(exporter.ID, null);
            }

            return result;
        }
    }
}
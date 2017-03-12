﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Oxide.Core;
using RustCalc.Common.Models;
using RustCalc.Common.Serializing;

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

            return true;
        }

        public static ExportData ExportData()
        {
            var result = new ExportData();

            foreach (var exporter in Exporters)
            {
                var exportData = exporter.ExportData(result);
                result.Add(exporter.ID, exportData);
            }

            Interface.Oxide.LogInfo($"Exporting {result.Items.Count} items, and {result.Recipes.Count} recipes.");
            
            return result;
        }

        public static void SerializeData(ExportData data, BinaryWriter writer)
        {
            Common.Models.ExportData.SetCurrent(data);

            writer.Write(data.Count);

            foreach (var kv in data)
            {
                writer.Write(kv.Key);
                writer.Write(kv.Value.GetType().FullName);
                kv.Value.Serialize(writer);
            }

            Common.Models.ExportData.SetCurrent(null);
        }

        public static ExportData DeserializeData(BinaryReader reader)
        {
            var result = new ExportData();
            Common.Models.ExportData.SetCurrent(result);
            int count = reader.ReadInt32();

            for (int i = 0; i < count; ++i)
            {
                string key = reader.ReadString();
                string typeName = reader.ReadString();

                Type type = Type.GetType(typeName);
                if (type == null) throw new ArgumentNullException(nameof(type));
                
                var instance = reader.Deserialize(type);

                result.Add(key, instance);
            }

            Common.Models.ExportData.SetCurrent(null);
            return result;
        }
    }
}
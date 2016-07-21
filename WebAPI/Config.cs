using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebAPI
{
    public static class Config
    {
        public static string UploadPassword { get; private set; } = CreatePassword(64); // This will effectively create a password everytime the program launches even if there's a saved password,
                                                                                        // but it's not exactly resource heavy and only happens once during startup, so it doesn't matter.

        private static void InitialSave()
        {
            // Generate random password.
            UploadPassword = CreatePassword(64);

            Save();
        }

        public static void Load()
        {
            if (!File.Exists("config.json"))
            {
                InitialSave();
                return;
            }

            string json = File.ReadAllText("config.json");
            var loadObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            if (loadObject == null)
            {
                Console.WriteLine("Failed to load config.json, is the file empty?");
                return;
            }

            var properties = typeof (Config).GetProperties(BindingFlags.Public | BindingFlags.Static);

            foreach (var property in properties)
            {
                if (loadObject.ContainsKey(property.Name))
                {
                    try
                    {
                        property.SetValue(null, loadObject[property.Name]);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to read config value for " + property.Name + ": " + ex.Message);
                    }
                }
            }

            Save(); // Will add new properties to older save files.
        }

        public static void Save()
        {
            using (StreamWriter file = File.CreateText("config.json"))
            {
                var saveObject = new Dictionary<string, object>();

                // Save all public static properties.
                foreach (var property in typeof (Config).GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    if (!property.CanWrite)
                        continue;

                    saveObject[property.Name] = property.GetValue(null);
                }

                file.Write(JsonConvert.SerializeObject(saveObject, Formatting.Indented));
            }
        }

        // http://stackoverflow.com/a/54997 (too lazy to write it myself)
        private static string CreatePassword(int length)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();

            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }

            return res.ToString();
        }
    }
}
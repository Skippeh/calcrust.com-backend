using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using WebAPI.Models.Destructibles;

namespace WebAPI.Models
{
    public class RustData : IDisposable
    {
        [JsonIgnore]
        public bool Disposed { get; private set; } = false;

        public Dictionary<string, Item> Items { get; set; }
        public Dictionary<string, Recipe> Recipes { get; set; }
        public Meta Meta { get; set; }
        public Dictionary<string, Cookable> Cookables { get; set; }
        public Dictionary<string, Destructible> DamageInfo { get; set; }

        public void Dispose()
        {
            Disposed = true;
        }

        public string GetBuildingBlockName(string prefabName)
        {
            switch (prefabName)
            {
                case "block.stair.lshape": return "L Shaped Stairs";
                case "block.stair.ushape": return "U Shaped Stairs";
                case "floor": return "Floor";
                case "floor.frame": return "Floor Frame";
                case "floor.triangle": return "Floor Triangle";
                case "foundation": return "Foundation";
                case "foundation.steps": return "Foundation Steps";
                case "foundation.triangle": return "Foundation Triangle";
                case "pillar": return "Pillar";
                case "roof": return "Roof";
                case "wall": return "Wall";
                case "wall.doorway": return "Doorway";
                case "wall.frame": return "Wall Frame";
                case "wall.low": return "Low Wall";
                case "wall.window": return "Window";
            }

            return prefabName;
        }
    }
}
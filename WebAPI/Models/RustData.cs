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
    }
}
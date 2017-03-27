using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RustCalc.Common.Models
{
    public class ItemSkinConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var result = new ItemSkin();
            JObject jSkin = JObject.Load(reader);

            result.Name = jSkin["name"].Value<string>();
            result.ItemDefId = jSkin["itemdefid"].Value<int>();
            result.Type = jSkin["type"].Value<string>();
            result.IconUrl = jSkin["icon_url"].Value<string>();
            result.IconUrlLarge = jSkin["icon_url_large"].Value<string>();
            result.Marketable = jSkin["marketable"].Value<bool>();
            result.Tradable = jSkin["tradable"].Value<bool>();
            result.Commodity = jSkin["commodity"].Value<bool>();
            result.MarketHashName = jSkin["market_hash_name"].Value<string>();
            result.MarketName = jSkin["market_name"].Value<string>();
            result.Description = jSkin["description"].Value<string>();
            result.ItemShortname = jSkin["itemshortname"].Value<string>();
            result.Tags = jSkin["tags"].Value<string>().Split(';');
            result.StoreTags = jSkin["store_tags"].Value<string>().Split(';');
            result.StoreHidden = jSkin["store_hidden"].Value<bool>();
            result.BackgroundColor = "#" + jSkin["background_color"].Value<string>();
            result.NameColor = "#" + jSkin["name_color"].Value<string>();

            return result;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (ItemSkin);
        }
    }

    public class ItemSkin
    {
        public string Name { get; set; }
        public int ItemDefId { get; set; }
        public string Type { get; set; }
        public string IconUrl { get; set; }
        public string IconUrlLarge { get; set; }
        public bool Marketable { get; set; }
        public bool Tradable { get; set; }
        public bool Commodity { get; set; }
        public string MarketHashName { get; set; }
        public string MarketName { get; set; }
        public string Description { get; set; }
        public string ItemShortname { get; set; }
        public string[] Tags { get; set; }
        public string[] StoreTags { get; set; }
        public bool StoreHidden { get; set; }
        public string BackgroundColor { get; set; }
        public string NameColor { get; set; }

        public void UpdateFrom(ItemSkin skin)
        {
            Name = skin.Name;
            ItemDefId = skin.ItemDefId;
            Type = skin.Type;
            IconUrl = skin.IconUrl;
            IconUrlLarge = skin.IconUrlLarge;
            Marketable = skin.Marketable;
            Tradable = skin.Tradable;
            Commodity = skin.Commodity;
            MarketHashName = skin.MarketHashName;
            MarketName = skin.MarketName;
            Description = skin.Description;
            ItemShortname = skin.ItemShortname;
            Tags = skin.Tags;
            StoreTags = skin.StoreTags;
            StoreHidden = skin.StoreHidden;
            BackgroundColor = skin.BackgroundColor;
            NameColor = skin.NameColor;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
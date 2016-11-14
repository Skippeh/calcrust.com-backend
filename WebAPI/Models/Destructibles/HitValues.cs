using Newtonsoft.Json;

namespace WebAPI.Models.Destructibles
{
    public class HitValues
    {
        [JsonProperty("strongDps")] public float StrongDPS;
        [JsonProperty("totalStrongHits")] public float TotalStrongHits;

        [JsonProperty("weakDps")] public float WeakDPS;
        [JsonProperty("totalWeakHits")] public float TotalWeakHits;

        [JsonProperty("totalStrongItems")] public float TotalStrongItems;
        [JsonProperty("totalWeakItems")] public float TotalWeakItems;
    }
}
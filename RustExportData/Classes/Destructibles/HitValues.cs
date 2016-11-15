using Newtonsoft.Json;

namespace Oxide.Classes.Destructibles
{
    public class HitValues
    {
        [JsonProperty("strongDps")] public float StrongDPS;
        [JsonProperty("totalStrongHits")] public float TotalStrongHits;

        [JsonProperty("weakDps")] public float WeakDPS;
        [JsonProperty("totalWeakHits")] public float TotalWeakHits;

        [JsonProperty("totalStrongItems")] public float TotalStrongItems = -1;
        [JsonProperty("totalWeakItems")] public float TotalWeakItems = -1;
    }
}
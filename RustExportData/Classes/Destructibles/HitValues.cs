using Newtonsoft.Json;

namespace Oxide.Classes.Destructibles
{
    public struct HitValues
    {
        [JsonProperty("strongDps")] public float StrongDPS;
        [JsonProperty("totalStrongHits")] public float TotalStrongHits;

        [JsonProperty("weakDps")] public float WeakDPS;
        [JsonProperty("totalWeakHits")] public float TotalWeakHits;
    }
}
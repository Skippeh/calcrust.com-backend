using Newtonsoft.Json;

namespace Oxide.Classes.Destructible
{
    public struct HitValues
    {
        [JsonProperty("dps")] public float DPS;
        [JsonProperty("totalHits")] public float TotalHits;
    }
}
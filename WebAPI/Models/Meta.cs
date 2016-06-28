using System;

namespace WebAPI.Models
{
    public class Meta
    {
        public DateTime LastUpdate;
        public float Time;

        public Meta(DateTime lastUpdate)
        {
            LastUpdate = lastUpdate;
        }

        public override string ToString()
        {
            return $"[Meta] LastUpdate: {LastUpdate} Time: {Time}";
        }
    }
}
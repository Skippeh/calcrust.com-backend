using System.Collections.Generic;
using System.Linq;

namespace WebAPI.Models.MetaModels
{
    public sealed class Burnable : ItemMeta
    {
        public float FuelAmount { get; set; }
        public int ByproductAmount { get; set; }
        public float ByproductChance { get; set; }
        public Item ByproductItem { get; set; }

        public Burnable(IEnumerable<string> descriptions) : base(MetaType.Burnable)
        {
            Descriptions = descriptions.ToList();
        }
    }
}
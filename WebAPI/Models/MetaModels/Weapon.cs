using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAPI.Models.MetaModels
{
    public sealed class Weapon : ItemMeta
    {
        public float FireDelay;
        public float ReloadTime;
        public int MagazineSize;

        public Weapon() : base(MetaType.Weapon)
        {
        }
    }
}

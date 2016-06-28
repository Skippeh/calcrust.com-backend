using System.Collections.Generic;

namespace WebAPI.Models
{
    public class ItemMeta
    {
        public virtual List<string> Descriptions { get; set; } = new List<string>();
    }
}
using System;
using System.Collections.Generic;

namespace RustCalc.Exporting
{
    public class ExporterAttribute : Attribute
    {
        public List<Type> Dependencies { get; set; }

        public ExporterAttribute(params Type[] dependencies)
        {
            Dependencies = new List<Type>(dependencies);
        }
    }
}
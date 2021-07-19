using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.DataContext.Catalog
{
    public class Reference
    {
        public int Manufacturer { get; set; }
        public int Id { get; set; }
        public int Version { get; set; }
        public string Additional { get; set; }


        public bool Equals(Reference comp)
        {
            return (Manufacturer == comp.Manufacturer && Id == comp.Id && Version == comp.Version);
        }
    }
}
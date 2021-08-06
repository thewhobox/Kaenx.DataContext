using Kaenx.DataContext.Import;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Catalog
{
    public class ApplicationViewModel
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(40)]
        public string Hash { get; set; }
        [MaxLength(100)]
        public string Name { get; set; }
        public int HardwareId { get; set; }
        public int Version { get; set; }
        public int Number { get; set; }
        [MaxLength(7)]
        public string Mask { get; set; }
        public int Manufacturer { get; set; }
        public bool IsRelativeSegment { get; set; }

        public int Table_Object { get; set; } = -1;
        public int Table_Object_Offset { get; set; }

        public int Table_Group { get; set; } = -1;
        public int Table_Group_Offset { get; set; }
        public int Table_Group_Max { get; set; }

        public int Table_Assosiations { get; set; } = -1;
        public int Table_Assosiations_Offset { get; set; }
        public int Table_Assosiations_Max { get; set; }

        public LoadProcedureTypes LoadProcedure { get; set; }


        public string VersionString
        {
            get
            {
                int rest = Version % 16;
                int full = (Version - rest) / 16;
                return "V" + full.ToString() + "." + rest.ToString();
            }
        }
    }

    public enum LoadProcedureTypes
    {
        Unknown,
        Default,
        Product,
        Merge,
        Konnekting
    }
}

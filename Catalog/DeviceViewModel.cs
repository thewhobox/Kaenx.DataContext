using Kaenx.DataContext.Import;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Catalog
{
    public class DeviceViewModel
    {
        [Key]
        public int Id { get; set; }
        public ImportTypes ImportType { get; set; }
        public int ManufacturerId { get; set; }
        [MaxLength(100)]
        public string Key { get; set; }
        [MaxLength(100)]
        public string Name { get; set; }
        [MaxLength(300)]
        public string VisibleDescription { get; set; }
        [MaxLength(100)]
        public string OrderNumber { get; set; }

        [NotMapped]
        public string ManufacturerName { get; set; }

        public bool HasIndividualAddress { get; set; }
        public bool HasApplicationProgram { get; set; }
        public bool IsPowerSupply { get; set; }
        public bool IsRailMounted { get; set; }
        public bool IsCoupler { get; set; }
        public int BusCurrent { get; set; }

        public int CatalogId { get; set; }
        public int HardwareId { get; set; }
    }
}

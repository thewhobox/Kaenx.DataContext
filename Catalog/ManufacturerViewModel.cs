using Kaenx.DataContext.Import;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Catalog
{
    public class ManufacturerViewModel
    {
        [Key]
        public int Id { get; set; }
        public int ManuId { get; set; }
        public ImportTypes ImportType { get; set; }
        [MaxLength(100)]
        public string Name { get; set; }
    }
}

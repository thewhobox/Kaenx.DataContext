using Kaenx.DataContext.Import;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Catalog
{
    public class CatalogViewModel
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(100)]
        public string Key { get; set; }
        public ImportTypes ImportType { get; set; }
        public int ParentId { get; set; }
        [MaxLength(100)]
        public string Name { get; set; }
    }
}

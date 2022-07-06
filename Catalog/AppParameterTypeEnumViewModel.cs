using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Catalog
{
    public class AppParameterTypeEnumViewModel
    {
        [Key]
        public int Id { get; set; }
        public int TypeId { get; set; }
        public int ParameterId { get; set; } //TODO what the heck? is it necsessary?
        [MaxLength(100)]
        public string Text { get; set; }
        [MaxLength(100)]
        public string Value { get; set; }
        public int Order { get; set; }
    }
}

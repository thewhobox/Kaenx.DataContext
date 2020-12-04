using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Kaenx.DataContext.Catalog
{
    public class AppAdditional
    {
        [Key]
        [MaxLength(18)]
        public string Id { get; set; }
        public byte[] LoadProcedures { get; set; }
        public byte[] Dynamic { get; set; }
        public byte[] Bindings { get; set; }
        public byte[] ParameterAll { get; set; }
        public byte[] ComsAll { get; set; }
        public byte[] ComsDefault { get; set; }
        public byte[] ParamsHelper { get; set; }
        public byte[] Assignments { get; set; }
    }
}

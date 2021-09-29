using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Kaenx.DataContext.Catalog
{
    public class AppAdditional
    {
        [Key]
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public byte[] LoadProcedures { get; set; }
        public byte[] Bindings { get; set; }
        public byte[] ComsAll { get; set; }
        public byte[] ComsDefault { get; set; }
        public byte[] ParamsHelper { get; set; }
        public byte[] Assignments { get; set; }
    }
}

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
        public byte[] ComsAll { get; set; } //TODO rename: ComBindings
        public byte[] ComsDefault { get; set; }
        public byte[] ParamsHelper { get; set; } //TODO rename: Channels
        public byte[] Assignments { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Catalog
{
    public class Baggage
    {
        [Key]
        public int UId { get; set; }
        public string Id { get; set; }
        public DateTime TimeStamp { get; set; }
        public byte[] Data { get; set; }
        public PictureTypes PictureType { get; set; }
    }

    public enum PictureTypes
    {
        PNG,
        JPG
    }
}

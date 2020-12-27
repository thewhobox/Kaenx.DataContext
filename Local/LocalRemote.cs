using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;

namespace Kaenx.DataContext.Local
{
    public class LocalRemote
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Host { get; set; }
        public string Authentification { get; set; }

        public string Description
        {
            get
            {
                return Host;
            }
        }
    }

}

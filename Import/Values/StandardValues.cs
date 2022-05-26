using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Import.Dynamic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Import.Values
{
    public class StandardValues : IValues
    {
        private string _value;

        public StandardValues(string value)
        {
            _value = value;
        }

        public string Value 
        {
           get {
                return _value; 
           }
        }
    }
}
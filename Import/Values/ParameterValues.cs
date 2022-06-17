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
    public class ParameterValues : IValues
    {
        public List<IDynParameter> Parameters = new List<IDynParameter>();
        public AssignParameter Assignment; //TODO get set; handler for parachanged
        public string Value 
        {
           get {
                IDynParameter para = Parameters.SingleOrDefault(p => p.IsVisible);
                return para == null ? "x" : para.Value; 
           }
        }
    }
}
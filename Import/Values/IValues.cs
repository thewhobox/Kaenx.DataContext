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
    public interface IValues
    {
        public string Value { get; }
    }
}
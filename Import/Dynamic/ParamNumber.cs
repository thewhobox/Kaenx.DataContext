using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Import.Dynamic
{
    public class ParamNumber : IDynParameter
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string SuffixText { get; set; }
        public string Default { get; set; }


        private string _value;
        public string Value
        {
            get { return _value; }
            set { if (string.IsNullOrEmpty(value)) return; _value = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value")); }
        }

        private bool _isVisible;
        public bool IsVisible
        {
            get { return _isVisible; }
            set { _isVisible = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsVisible")); }
        }

        [JsonProperty("mi")]
        public int Minimum { get; set; }
        [JsonProperty("ma")]
        public int Maximum { get; set; }

        public bool HasAccess { get; set; }
        public bool IsEnabled { get; set; } = true;
        public List<ParamCondition> Conditions { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Import.Dynamic
{
    public class ParamEnum : IDynParameter
    {
        public long Id { get; set; }
        public string Text { get; set; }
        public string SuffixText { get; set; }
        public string Default { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public int DisplayOrder { get; set; }

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
            set
            {
                _isVisible = value;
                if (_isVisible != value)
                    Changed("ParamVisibility");
                Changed("IsVisible");
            }
        }

        private bool _isVisibleCondition;
        public bool IsVisibleCondition
        {
            get { return _isVisibleCondition; }
            set { _isVisibleCondition = value; Changed("IsVisibleCondition"); }
        }
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public bool HasAccess { get; set; }
        [JsonProperty("o")]
        public List<ParamEnumOption> Options { get; set; } = new List<ParamEnumOption>();
        public bool IsEnabled { get; set; } = true;
        public List<ParamCondition> Conditions { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}

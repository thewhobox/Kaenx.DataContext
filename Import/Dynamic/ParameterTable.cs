using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Import.Dynamic
{
    public class ParameterTable: INotifyPropertyChanged, IDynParameter
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        [JsonProperty("i")]
        public long Id { get; set; }
        [JsonProperty("a")]
        public bool HasAccess { get; set; } = true;
        [JsonProperty("t")]
        public string Text { get; set; }
        [JsonProperty("s")]
        public string SuffixText { get; set; }
        [JsonProperty("v")]
        public string Value { get; set; }

        [JsonProperty("d")]
        public string Default { get; set; }
        [JsonProperty("e")]
        public bool IsEnabled { get; set; }


        private bool _isVisible;
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (_isVisible != value)
                    Changed("ParamVisibility");
                _isVisible = value;
                Changed("IsVisible");
            }
        }

        [JsonProperty("vc")]
        private bool _isVisibleCondition;
        public bool IsVisibleCondition
        {
            get { return _isVisibleCondition; }
            set { _isVisibleCondition = value; Changed("IsVisibleCondition"); }
        }


        public List<int> Columns {get;set;} = new List<int>();
        public List<int> Rows {get;set;} = new List<int>();

        [JsonProperty("c")]
        public List<ParamCondition> Conditions { get; set; } = new List<ParamCondition>();
        [JsonProperty("p")]
        public List<IDynParameter> Parameters { get; set; } = new List<IDynParameter>();
    }
}
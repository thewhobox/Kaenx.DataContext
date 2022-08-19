using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Kaenx.DataContext.Import.Dynamic
{
    public class ParamSlider : IDynParameter
    {
        public long Id { get; set; }
        public string Text { get; set; }
        public string SuffixText { get; set; }
        public string Default { get; set; }
        public ParamSeparatorHint Hint { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public int DisplayOrder { get; set; }

        private string _value;
        public string Value
        {
            get { return _value; }
            set { 
                _value = value; 
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value")); 
                }
        }
        private bool _isVisible;
        public bool IsVisible
        {
            get { return _isVisible; }
            set { _isVisible = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsVisible")); }
        }

        [JsonIgnore]
        [JsonProperty("l")]
        public bool IsLineVisible { get { return string.IsNullOrEmpty(Text) || string.IsNullOrWhiteSpace(Text); } }

        [JsonProperty("mi")]
        public double Minimum { get; set; }
        [JsonProperty("ma")]
        public double Maximum { get; set; }
        public double Increment { get; set; }

        public bool HasAccess { get; set; } = true;
        public List<ParamCondition> Conditions { get; set; }
        public bool IsEnabled { get; set; }
        
        private bool _isVisibleCondition;
        public bool IsVisibleCondition
        {
            get { return _isVisibleCondition; }
            set { _isVisibleCondition = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsVisibleCondition")); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Import.Dynamic
{
    public class ParamEnumTwo : IDynParameter
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string SuffixText { get; set; }
        public string Default { get; set; }

        private string _value;
        public string Value
        {
            get { return _value; }
            set {
                if (string.IsNullOrEmpty(value)) return; 
                _value = value;
                _selected1 = _value == Option1?.Value;
                _selected2 = _value == Option2?.Value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Selected1"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Selected2"));
            }
        }

        private bool _isVisible;
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                bool old = _isVisible;
                _isVisible = value;
                if (old != value)
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

        [JsonIgnore]
        private bool _selected1;
        [JsonIgnore]
        public bool Selected1
        {
            get { return _selected1; }
            set { 
                _selected1 = value;
                if (_selected1) Value = Option1.Value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Selected1")); 
            }
        }

        [JsonIgnore]
        private bool _selected2;
        [JsonIgnore]
        public bool Selected2
        {
            get { return _selected2; }
            set { 
                _selected2 = value;
                if (_selected2) Value = Option2.Value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Selected2")); 
            }
        }

        private ParamEnumOption _opt1;
        [JsonProperty("o1")]
        public ParamEnumOption Option1
        {
            get { return _opt1; }
            set { _opt1 = value; if (_opt1.Value == Value) Selected1 = true; }
        }
        private ParamEnumOption _opt2;
        [JsonProperty("o2")]
        public ParamEnumOption Option2
        {
            get { return _opt2; }
            set { _opt2 = value; if (_opt2.Value == Value) Selected2 = true; }
        }

        public bool HasAccess { get; set; }
        public bool IsEnabled { get; set; } = true;
        public List<ParamCondition> Conditions { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}

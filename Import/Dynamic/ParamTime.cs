using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Import.Dynamic
{
    public class ParamTime : IDynParameter
    {
        public long Id { get; set; }
        public string Text { get; set; }
        public string SuffixText { get; set; }
        public string Default { get; set; }
        public int Divider { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public int DisplayOrder { get; set; }
        public string Helptext { get; set; }

        public string TempValue
        {
            get {
                return (int.Parse(Value) * Divider).ToString();
            }
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TempValue"));
                _value = (Math.Floor((double)int.Parse(value) / Divider)).ToString();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
            }
        }

        private string _value;
        public string Value
        {
            get { return _value; }
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TempValue"));
            }
        }


        //private string _tempValue;
        //public string TempValue
        //{
        //    get { return _tempValue; }
        //    set {
        //        if (string.IsNullOrEmpty(value)) return;
        //        _tempValue = value; 
        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TempValue"));
        //        _value = (Math.Floor((double)int.Parse(value) / Divider)).ToString();
        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
        //    }
        //}

        //private string _value;
        //public string Value
        //{
        //    get { return _value; }
        //    set { 
        //        if (string.IsNullOrEmpty(value)) return; 
        //        _value = value; 
        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));

        //        _tempValue = (int.Parse(value) * Divider).ToString();
        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TempValue"));
        //    }
        //}

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

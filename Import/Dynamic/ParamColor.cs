using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Import.Dynamic
{
    public class ParamColor : IDynParameter
    {
        public long Id { get; set; }
        public string Text { get; set; }
        public string SuffixText { get; set; }
        public string Default { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public int DisplayOrder { get; set; }

        private string _color;
        public string Value
        {
            get { return _color; }
            set { _color = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value")); }
        }

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

        public bool SuffixIsVisible { get { return !string.IsNullOrEmpty(SuffixText); } }

        public bool HasAccess { get; set; }
        public bool IsEnabled { get; set; } = true;
        public List<ParamCondition> Conditions { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

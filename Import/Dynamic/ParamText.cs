using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Import.Dynamic
{
    public class ParamText : IDynParameter
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string Hash { get; set; }
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
            set { _isVisible = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UsVisible")); }
        }

        public int MaxLength { get; set; }

        public bool SuffixIsVisible { get { return !string.IsNullOrEmpty(SuffixText); } }

        public bool HasAccess { get; set; }
        public bool IsEnabled { get; set; } = true;
        public List<ParamCondition> Conditions { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}

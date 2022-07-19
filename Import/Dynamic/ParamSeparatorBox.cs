using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Import.Dynamic
{
    public class ParamSeparatorBox : IDynParameter
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string SuffixText { get; set; }
        public string Default { get; set; }
        public ParamSeparatorHint Hint { get; set; }

        public string Value { get; set; }

        private bool _isVisible;
        public bool IsVisible
        {
            get { return _isVisible; }
            set { _isVisible = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsVisible")); }
        }

        public bool IsError { get; set; }

        public bool SuffixIsVisible { get { return false; } }

        public bool HasAccess { get; set; } = true;
        public List<ParamCondition> Conditions { get; set; }
        public bool IsEnabled { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}

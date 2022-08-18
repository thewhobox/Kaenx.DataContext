using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Import.Dynamic
{
    public class ChannelBlock : IDynChannel, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public long Id { get; set; }
        public string Name { get; set; }
        public string DefaultText { get; set; }
        public bool HasAccess { get; set; } = true;
        public int Number { get; set; }

        private string _text;
        public string Text
        {
            get { return _text; }
            set { _text = value; Changed("Text"); }
        }

        public List<ParameterBlock> Blocks { get; set; } = new List<ParameterBlock>();
        public List<ParamCondition> Conditions { get; set; } = new List<ParamCondition>();

        private bool _isVisible;
        public bool IsVisible
        {
            get { return _isVisible; }
            set { _isVisible = value; Changed("IsVisible"); }
        }

    }
}

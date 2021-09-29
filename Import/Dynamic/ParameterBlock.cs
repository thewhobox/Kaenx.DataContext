using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Import.Dynamic
{
    public class ParameterBlock: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        [JsonProperty("i")]
        public int Id { get; set; }
        [JsonProperty("d")]
        public string DefaultText { get; set; }
        [JsonProperty("a")]
        public bool HasAccess { get; set; } = true;

        private string _text;
        [JsonProperty("t")]
        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                Changed("Text");
            }
        }


        private bool _isVisible;
        [JsonProperty("v")]
        public bool IsVisible
        {
            get { return _isVisible; }
            set { 
                _isVisible = value; 
                Changed("IsVisible"); 
            }
        }

        [JsonProperty("c")]
        public List<ParamCondition> Conditions { get; set; } = new List<ParamCondition>();
        [JsonProperty("p")]
        public List<IDynParameter> Parameters { get; set; } = new List<IDynParameter>();
        [JsonProperty("b")]
        public List<ParameterBlock> Blocks { get; set; } = new List<ParameterBlock>();
    }
}

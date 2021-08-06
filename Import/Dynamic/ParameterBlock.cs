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
        [JsonProperty("t")]
        public string Text { get; set; }
        [JsonProperty("d")]
        public string DefaultText { get; set; }
        [JsonProperty("a")]
        public bool HasAccess { get; set; } = true;

        private string _dtext;
        [JsonProperty("di")]
        public string DisplayText
        {
            get { return _dtext; }
            set
            {
                _dtext = value;
                Changed("DisplayText");
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
    }
}

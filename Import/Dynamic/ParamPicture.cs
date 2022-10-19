using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Import.Dynamic
{
    public class ParamPicture : IDynParameter
    {
        public delegate object PictureRequestHandler(string BaggageId);
        public event PictureRequestHandler OnPictureRequest;

        public long Id { get; set; }
        public string Text { get; set; }
        public string SuffixText { get; set; }
        public string Default { get; set; }
        public ParamSeparatorHint Hint { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public int DisplayOrder { get; set; }
        public string Helptext { get; set; }

        public string Value { get; set; }
        public string BaggageId { get; set; }

        [JsonIgnore]
        public object _image;
        public object Image
        {
            get
            {
                return OnPictureRequest?.Invoke(BaggageId);
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

        public bool HasAccess { get; set; } = true;
        public List<ParamCondition> Conditions { get; set; }
        public bool IsEnabled { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

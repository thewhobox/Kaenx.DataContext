using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Import.Dynamic
{
    public interface IDynParameter: INotifyPropertyChanged
    {
        [JsonProperty("i")]
        long Id { get; set; }
        [JsonProperty("t")]
        string Text { get; set; }
        [JsonProperty("s")]
        string SuffixText { get; set; }
        [JsonProperty("v")]
        string Value { get; set; }

        [JsonProperty("d")]
        string Default { get; set; }

        [JsonProperty("a")]
        bool HasAccess { get; set; }
        [JsonProperty("e")]
        bool IsEnabled { get; set; }
        [JsonProperty("vi")]
        bool IsVisible { get; set; }
        [JsonProperty("c")]
        List<ParamCondition> Conditions { get; set; }
        int DisplayOrder { get; set; }
    }
}

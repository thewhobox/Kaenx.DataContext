using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Import.Dynamic
{
    public class ParamBinding
    {
        [JsonProperty("s")]
        public int SourceId { get; set; } = -2;
        [JsonProperty("t")]
        public int TargetId { get; set; } = -2;
        [JsonProperty("d")]
        public string DefaultText { get; set; }
        [JsonProperty("f")]
        public string FullText { get; set; }
        [JsonProperty("b")]
        public BindingTypes Type { get; set; }
    }

    public enum BindingTypes {
        Channel,
        ComObject,
        ParameterBlock
    }
}
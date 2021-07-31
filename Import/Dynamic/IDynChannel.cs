﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Import.Dynamic
{
    public interface IDynChannel
    {
        public int Id { get; set; }
        [JsonProperty("ha")]
        public bool HasAccess { get; set; }
        [JsonProperty("bl")]
        public List<ParameterBlock> Blocks { get; set; }
        [JsonProperty("vi")]
        public bool IsVisible { get; set; }

        [JsonProperty("co")]
        public List<ParamCondition> Conditions { get; set; }
    }
}

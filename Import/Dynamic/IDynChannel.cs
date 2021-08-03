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
        //TODO set public
        int Id { get; set; }
        [JsonProperty("ha")]
        bool HasAccess { get; set; }
        [JsonProperty("bl")]
        List<ParameterBlock> Blocks { get; set; }
        [JsonProperty("vi")]
        bool IsVisible { get; set; }

        [JsonProperty("co")]
        List<ParamCondition> Conditions { get; set; }
    }
}

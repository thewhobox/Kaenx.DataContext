﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Import.Dynamic
{
    public class ParamCondition
    {

        [JsonProperty("s")]
        public long SourceId { get; set; }
        [JsonProperty("v")]
        public string Values { get; set; }
        [JsonProperty("o")]
        public ConditionOperation Operation { get; set; }

        public ParamCondition() { }
    }

    public enum ConditionOperation
    {
        IsInValue,
        Default,
        GreatherThan,
        GreatherEqualThan,
        LowerThan,
        LowerEqualThan,
        NotEqual,
        Equal
    }

}

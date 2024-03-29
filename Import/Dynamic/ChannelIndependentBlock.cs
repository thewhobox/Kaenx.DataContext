﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Import.Dynamic
{
    public class ChannelIndependentBlock : IDynChannel
    {
        public long Id { get; set; }
        public bool HasAccess { get; set; } = true;
        public List<ParameterBlock> Blocks { get; set; } = new List<ParameterBlock>();
        public bool IsVisible { get; set; }

        public List<ParamCondition> Conditions { get; set; } = new List<ParamCondition>();
    }
}

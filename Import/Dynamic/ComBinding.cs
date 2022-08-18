﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.DataContext.Import.Dynamic
{
    public class ComBinding
    {
        public long ComId { get; set; }
        public List<ParamCondition> Conditions { get; set; } = new List<ParamCondition> ();
    }
}

﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Catalog
{
    public class AppParameterTypeViewModel
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(70)]
        public string Name { get; set; }
        public int ApplicationId { get; set; }
        public ParamTypes Type { get; set; }
        public int Size { get; set; }
        [MaxLength(100)]
        public string Tag1 { get; set; }
        [MaxLength(100)]
        public string Tag2 { get; set; }
    }

    public enum ParamTypes
    {
        Text,
        Enum,
        NumberUInt,
        NumberInt,
        Float9,
        Picture,
        None,
        IpAdress,
        Color,
        CheckBox,
        Time,
        NumberHex,
        Slider
    }
}

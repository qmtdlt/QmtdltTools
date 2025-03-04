﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QmtdltTools.Domain.Models
{
    public class Response<T>
    {
        public T data { get; set; }
        public int code { get; set; }
        public string? message { get; set; }
    }
}

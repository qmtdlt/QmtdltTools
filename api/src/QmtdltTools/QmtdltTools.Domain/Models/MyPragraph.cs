using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QmtdltTools.Domain.Models
{
    public class MyPragraph
    {
        // 段落文本
        public string PragraphText { get; set; }
        // 段落句子
        public List<string> Sentences { get; set; }
    }
}

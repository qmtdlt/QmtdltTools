using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersOne.Epub;

namespace QmtdltTools.Domain.Models
{
    public class BookReaderModel
    {
        public int position { get; set; }           // 位置
        public EpubBook ebook { get; set; }         // 电子书对象
    }

    public class UIReadInfo
    {
        public string text { get; set; }
        public byte[] buffer { get; set; }
        public int position { get; set; }
    }
}

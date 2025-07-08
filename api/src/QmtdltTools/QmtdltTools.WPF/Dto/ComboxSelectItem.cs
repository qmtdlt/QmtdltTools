using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QmtdltTools.WPF.Dto
{
    public class ComboxSelectItem<T> where T : Enum
    {
        public T id { get; set; }
        public string text { get; set; }
    }
    public class ComboxSelectItemCs<T> where T : struct
    {
        public T id { get; set; }
        public string text { get; set; }
    }

    public class ComboxSelectItemStringCs<T> where T : class // 或者不加约束
    {
        public string id { get; set; }
        public T text { get; set; } // 允许 T 是 string?
    }
}

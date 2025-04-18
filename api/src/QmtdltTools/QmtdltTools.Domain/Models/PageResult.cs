using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QmtdltTools.Domain.Models
{
    public class PageResult<T> where T : class
    {
        public List<T> PageList { get; set; }
        public int Total { get; set; }
        public int PageCount { get; set; }
    }
}

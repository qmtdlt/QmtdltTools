using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QmtdltTools.Domain.Entitys
{
    [Table("DayToDo")]
    public class DayToDo:EntityBaseId
    {
        public string? Content { get; set; }
        public bool? IsFinish { get; set; }
        public DateTime? FinishTime { get; set; }
        public long? SortBy { get; set; }
        public bool? InCurrent { get; set; }            // 是否处于当前待办
    }
}

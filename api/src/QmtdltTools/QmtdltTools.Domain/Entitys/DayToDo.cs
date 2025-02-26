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
        public string Content { get; set; }
        public DateTime FinishTime { get; set; }
    }
}

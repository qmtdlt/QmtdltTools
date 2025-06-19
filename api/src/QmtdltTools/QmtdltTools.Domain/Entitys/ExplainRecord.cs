using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QmtdltTools.Domain.Entitys
{
    [Table("explainrecord")]
    public class ExplainRecord : EntityBaseId
    {
        public Guid? BookId { get; set; }
        public int? PhaseIndex { get; set; }
        public string? Phase { get; set; }
        public string? Explanation { get; set; }
        public byte[]? VoiceBuffer { get; set; }
    }
}

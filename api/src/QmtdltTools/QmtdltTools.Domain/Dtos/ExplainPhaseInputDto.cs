using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QmtdltTools.Domain.Dtos
{
    public class ExplainPhaseInputDto
    {
        public Guid bookId { get; set; }
        public string Phase { get; set; }
        public int PhaseIndex { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QmtdltTools.Domain.Dtos
{
    public class TranslateDto
    {
        public string Explanation { get; set; }
        public string Translation { get; set; }
        public byte[] VoiceBuffer { get; set; }
        public byte[] WordVoiceBuffer { get; set; }
        public byte[] SentenceVoiceBuffer { get; set; }
    }
}

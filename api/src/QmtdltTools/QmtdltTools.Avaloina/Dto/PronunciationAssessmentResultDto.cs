using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QmtdltTools.Avaloina.Dto
{
    public class PronunciationAssessmentResultDto
    {
        public double AccuracyScore { get; set; }
        public double PronunciationScore { get; set; }
        public double CompletenessScore { get; set; }
        public double FluencyScore { get; set; }
        public double ProsodyScore { get; set; }
        public List<WordResultDto>? Words { get; set; }
    }

    public class WordResultDto
    {
        public string Word { get; set; } = "";
        public double AccuracyScore { get; set; }
        public string ErrorType { get; set; } = "";
        public List<SyllableDto>? Syllables { get; set; }
        public List<PhonemeDto>? Phonemes { get; set; }
    }

    public class SyllableDto
    {
        public string Syllable { get; set; } = "";
        public double AccuracyScore { get; set; }
        public string? Grapheme { get; set; }
    }

    public class PhonemeDto
    {
        public string Phoneme { get; set; } = "";
        public double AccuracyScore { get; set; }
    }

}

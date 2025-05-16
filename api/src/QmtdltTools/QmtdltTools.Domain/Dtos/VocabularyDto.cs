using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QmtdltTools.Domain.Dtos
{
    public class VocabularyDto
    {
        public Guid Id { get; set; }                        // 表Id
        public Guid VocabularyId { get; set; }              // 词汇Id
        public string? SentenceYouMade { get; set; }
        public byte[]? SentencePronunciation { get; set; }
        public bool? IfUsageCorrect { get; set; }
        public string? IncorrectReason { get; set; }
        public string? CorrectSentence { get; set; }
        public DateTime? CreateTime { get; set; }

        public string? WordText { get; set; }
        public byte[]? WordPronunciation { get; set; }
        public string? AIExplanation { get; set; }
        public byte[]? Pronunciation { get; set; }
        public string? AITranslation { get; set; }
    }
}

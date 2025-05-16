using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QmtdltTools.Domain.Entitys
{

    [Table("UserVocabulary")]
    public class UserVocabulary:EntityBaseId
    {
        public Guid VocabularyId { get; set; }
        /// <summary>
        /// 你的造句
        /// </summary>
        [StringLength(512)]
        public string? SentenceYouMade { get; set; }
        public byte[]? SentencePronunciation { get; set; }

        /// <summary>
        /// 造句是否正确
        /// </summary>
        public bool? IfUsageCorrect { get; set; }
        /// <summary>
        /// 不正确原因
        /// </summary>
        [StringLength(1024)]
        public string? IncorrectReason { get; set; }
        /// <summary>
        /// 纠正后的句子
        /// </summary>
        [StringLength(256)]
        public string? CorrectSentence { get; set; }
    }
}

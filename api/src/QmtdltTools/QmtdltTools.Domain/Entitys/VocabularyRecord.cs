using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QmtdltTools.Domain.Entitys
{
    [Table("VocabularyRecord")]
    public class VocabularyRecord : EntityBaseId
    {
        public Guid BookId { get; set; }
        /// <summary>
        /// 单词文本
        /// </summary>
        [StringLength(256)]
        public string? WordText { get; set; }
        public byte[]? WordPronunciation { get; set; }
        
        /// <summary>
        /// AI 解释
        /// </summary>
        [StringLength(1024)]
        public string? AIExplanation { get; set; }
        /// <summary>
        /// 发音
        /// </summary>
        public byte[]? Pronunciation { get; set; }
        /// <summary>
        /// AI 翻译
        /// </summary>
        [StringLength(1024)]
        public string? AITranslation { get; set; }
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

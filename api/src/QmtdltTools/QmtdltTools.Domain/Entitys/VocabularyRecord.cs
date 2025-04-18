using System;
using System.Collections.Generic;
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
        public int PIndex { get; set; }
        /// <summary>
        /// 单词文本
        /// </summary>
        public string? WordText { get; set; }
        /// <summary>
        /// 发音
        /// </summary>
        public byte[]? Pronunciation { get; set; }
        /// <summary>
        /// AI 解释
        /// </summary>
        public string? AIExplanation { get; set; }
        /// <summary>
        /// AI 翻译
        /// </summary>
        public string? AITranslation { get; set; }
        /// <summary>
        /// 你的第一个造句
        /// </summary>
        public string? FirstSentenceYouMade { get; set; }
        /// <summary>
        /// 你的第二个造句
        /// </summary>
        public string? SecondSentenceYouMade { get; set; }
        /// <summary>
        /// 你的第三个造句
        /// </summary>
        public string? ThirdSentenceYouMade { get; set; }
    }
}

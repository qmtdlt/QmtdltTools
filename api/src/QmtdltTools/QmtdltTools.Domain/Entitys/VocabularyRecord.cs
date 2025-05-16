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
        //public Guid BookId { get; set; }
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
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QmtdltTools.Domain.Entitys
{
    /// <summary>
    /// 后续可统计句子听写次数
    /// </summary>
    [Table("ListenWriteRecord")]
    public class ListenWriteRecord : EntityBaseId
    {
        public Guid BookId { get; set; }
        public int PIndex { get; set; }
        public int SIndex { get; set; }
        [StringLength(1024)]
        public string? SentenceText { get; set; }
        public byte[]? Pronunciation { get; set; }
    }
}

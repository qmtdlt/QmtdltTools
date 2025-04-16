using System.ComponentModel.DataAnnotations;

namespace QmtdltTools.Domain.Entitys;

public class EntityBaseId
{
    [Key]
    public Guid Id { get; set; }
    public DateTime? CreateTime { get; set; }
    public Guid? CreateBy { get; set; }
    public DateTime? UpdateTime { get; set; }
    public Guid? UpdateBy { get; set; }
}
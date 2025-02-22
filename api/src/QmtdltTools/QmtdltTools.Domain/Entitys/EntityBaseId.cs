using System.ComponentModel.DataAnnotations;

namespace QmtdltTools.Domain.Entitys;

public class EntityBaseId
{
    [Key]
    public Guid Id { get; set; }
    public DateTime? CreateTime { get; set; }
    public string? CreateBy { get; set; }
    public DateTime? UpdateTime { get; set; }
    public string? UpdateBy { get; set; }
}
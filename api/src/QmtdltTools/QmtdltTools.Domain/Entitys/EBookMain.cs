using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QmtdltTools.Domain.Entitys;

[Table("EBookMain")]
public class EBookMain:EntityBaseId
{
    [StringLength(256)]
    public string? Title { get; set; }
    [StringLength(256)]
    public string? Author { get; set; }
    [StringLength(256)]
    public string? Description { get; set; }
    public byte[]? CoverImage { get; set; }         // base64
    [StringLength(256)]
    public string? BookPath { get; set; }           // 
    public string? BookType { get; set; }           // 电子书类型
}
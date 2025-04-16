using System.ComponentModel.DataAnnotations.Schema;

namespace QmtdltTools.Domain.Entitys;

[Table("EBookMain")]
public class EBookMain:EntityBaseId
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Description { get; set; }
    public string? CoverImage { get; set; }         // base64
    public string? BookPath { get; set; }           // 
}
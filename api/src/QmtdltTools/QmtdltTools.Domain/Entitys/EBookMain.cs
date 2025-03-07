using System.ComponentModel.DataAnnotations.Schema;

namespace QmtdltTools.Domain.Entitys;

[Table("EBookMain")]
public class EBookMain:EntityBaseId
{
    public string Title { get; }

    public string Author { get; }

    public string? Description { get; }

    public byte[]? CoverImage { get; }

    public Guid CurSubId { get; set; }
}
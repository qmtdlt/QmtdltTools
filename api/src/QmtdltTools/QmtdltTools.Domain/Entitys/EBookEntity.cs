using System.ComponentModel.DataAnnotations.Schema;

namespace QmtdltTools.Domain.Entitys;

[Table("EBook")]
public class EBookEntity:EntityBaseId
{
    /// <summary>
    /// 书名
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 书封面图base64字符串
    /// </summary>
    public string CoverImageBase64 { get; set; }
    
    /// <summary>
    /// book 二进制文件
    /// </summary>
    public byte[] BookBin { get; set; }
}
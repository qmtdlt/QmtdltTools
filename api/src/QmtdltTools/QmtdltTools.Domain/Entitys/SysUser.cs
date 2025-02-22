using System.ComponentModel.DataAnnotations.Schema;

namespace QmtdltTools.Domain.Entitys;


[Table("SysUser")]
public class SysUser : EntityBaseId
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? IsActive { get; set; }
}
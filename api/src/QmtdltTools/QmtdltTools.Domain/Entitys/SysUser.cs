using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QmtdltTools.Domain.Entitys;


[Table("SysUser")]
public class SysUser : EntityBaseId
{
    [StringLength(256)]
    public string? Name { get; set; }
    [StringLength(256)]
    public string? Code { get; set; }
    [StringLength(256)]
    public string? Email { get; set; }
    [StringLength(256)]
    public string? PasswordHash { get; set; }
    [StringLength(256)]
    public string? PhoneNumber { get; set; }
    public bool? IsActive { get; set; }
    // ÊÇ·ñÓÎ¿Í
    public bool? IsGuest { get; set; }
}
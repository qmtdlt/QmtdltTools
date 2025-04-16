using Microsoft.AspNetCore.Mvc.Controllers;
using QmtdltTools.Domain.Entitys;

namespace QmtdltTools.Service.Utils;

public static class HttpExtension
{
    public static SysUser GetCurUser(this HttpContext context)
    {
        var userId = HttpContext.User.Claims.ToList().FirstOrDefault()?.Value;
    }
}
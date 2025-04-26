using Microsoft.AspNetCore.SignalR;
using Volo.Abp.AspNetCore.SignalR;

namespace QmtdltTools.Hubs;

public static class HubExtensions
{
    public static string GetUserIdStr(this HubCallerContext context)
    {
        var userId = context.User.Claims.ToList().FirstOrDefault()?.Value;
        return userId;
    }
    
    public static Guid? GetUserId(this HubCallerContext context)
    {
        var userId = context.User.Claims.ToList().FirstOrDefault()?.Value;
        if (userId.IsNullOrEmpty())
            return null;
        return new Guid(userId);
    }
}
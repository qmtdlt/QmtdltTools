namespace QmtdltTools.Extensions;

public static class HttpContextExt
{
    public static string GetUserIdStr(this HttpContext context)
    {
        var userId = context.User.Claims.ToList().FirstOrDefault()?.Value;
        return userId;
    }
    public static Guid? GetUserId(this HttpContext context)
    {
        var userId = context.User.Claims.ToList().FirstOrDefault()?.Value;
        if (userId.IsNullOrEmpty())
            return null;
        return new Guid(userId);
    }
}
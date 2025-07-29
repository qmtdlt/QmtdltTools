using System;

namespace QmtdltTools.Avaloina.Utils;

public static class WebHelper
{
    public static byte[] Base64ToBytes(this string base64String)
    {
        if (string.IsNullOrEmpty(base64String))
            return Array.Empty<byte>();
        try
        {
            return Convert.FromBase64String(base64String);
        }
        catch (FormatException)
        {
            // 如果转换失败，返回空字节数组
            return Array.Empty<byte>();
        }
    }
}
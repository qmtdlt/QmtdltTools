namespace QmtdltTools.Domain.Data;

public class ApplicationConst
{
    public static string LogPath
    { 
        get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
            "Logs", $"{DateTime.Now:yyyyMMddHHmmss}logs.txt"); } 
    }
}
namespace QmtdltTools.Domain.Data;

public class ApplicationConst
{
    public static string LogPath
    { 
        get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
            "Logs", $"{DateTime.Now:yyyyMMddHHmmss}logs.txt"); } 
    }
    public const string DefaultVoiceName = "en-US-AvaMultilingualNeural";
    public static string SPEECH_KEY = "";
    public static string SPEECH_REGION = "";
    public static string GROK_KEY = "";
}
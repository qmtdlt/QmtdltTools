namespace QmtdltTools.Domain.Data;

public class ApplicationConst
{
    public const string TimeFormat = "yyyyMMddHHmmss";
    public static string LogPath
    { 
        get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
            "Logs", $"{DateTime.Now.ToString(TimeFormat)}logs.txt"); } 
    }
    //public const string DefaultVoiceName = "en-US-AvaMultilingualNeural";
    public const string DefaultVoiceName = "zh-CN-XiaochenMultilingualNeural";
    public static string SPEECH_KEY = "";
    public static string SPEECH_REGION = "";
    public static string GROK_KEY = "";
    public static string GEMINI_KEY = "";
    public static string DOU_BAO = "";
    public static string QIAN_WEN = "";    
}
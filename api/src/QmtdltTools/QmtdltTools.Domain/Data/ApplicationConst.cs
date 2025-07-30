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
    public static int AIType = 4;
    public const string IsGuest = "IsGuest";

    public const double GuestListenTimeLimit = 10;          // 3 * 3600 为总秒数，可以听书3小时
    public const int QuotaBookCnt = 3;                  // 每月可以上传3本书
    public const int QuotaTranslateWordsCnt = 11;          // 每月可以翻译100个词汇
    public const int QuotaExplainParagraph = 1;          // 每月可以解释3个段落
}
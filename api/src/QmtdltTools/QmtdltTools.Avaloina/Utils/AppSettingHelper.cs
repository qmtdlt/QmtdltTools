using System;
using System.Collections.Generic;
using System.IO;

namespace QmtdltTools.Avaloina.Utils;

public static class AppSettingHelper
{
    public static string OpenSubtitleApiKey
    {
        get => GetValue(nameof(OpenSubtitleApiKey), "");
        set => SetValue(nameof(OpenSubtitleApiKey), value);
    }
    public static long LastVideoProgress
    {
        get => long.TryParse(GetValue(nameof(LastVideoProgress), "0"), out var v) ? v : 0;
        set => SetValue(nameof(LastVideoProgress), value.ToString());
    }

    public static string LastVideoPath
    {
        get => GetValue(nameof(LastVideoPath), "");
        set => SetValue(nameof(LastVideoPath), value);
    }

    public static string LastVideoSrt
    {
        get => GetValue(nameof(LastVideoSrt), "");
        set => SetValue(nameof(LastVideoSrt), value);
    }
    public static string ApiServer
    {
        get => GetValue(nameof(ApiServer), "https://youngforyou.top:5083");
        set => SetValue(nameof(ApiServer), value);
    }

    public static string OffLineCfg
    {
        get => GetValue(nameof(OffLineCfg), "");
        set => SetValue(nameof(OffLineCfg), value);
    }
    
    public static string LoginUserName
    {
        get => GetValue(nameof(LoginUserName), "");
        set => SetValue(nameof(LoginUserName), value);
    }
    public static string LoguserPwd
    {
        get => GetValue(nameof(LoguserPwd), "");
        set => SetValue(nameof(LoguserPwd), value);
    }
    #region 核心实现

    private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "conf.txt");
    private static readonly object _lock = new();
    private static Dictionary<string, string> _settings;

    static AppSettingHelper()
    {
        Load();
    }

    private static void Load()
    {
        _settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // ✅ 1. 如果配置文件不存在则创建
        if (!File.Exists(ConfigPath))
        {
            File.Create(ConfigPath).Close(); // 创建空文件
            return;
        }

        var lines = File.ReadAllLines(ConfigPath);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            var index = line.IndexOf('=');
            if (index <= 0)
                continue;

            var key = line.Substring(0, index).Trim();
            var value = line.Substring(index + 1).Trim();
            _settings[key] = value;
        }
    }

    private static void Save()
    {
        lock (_lock)
        {
            var lines = new List<string>();
            foreach (var kvp in _settings)
            {
                lines.Add($"{kvp.Key}={kvp.Value}");
            }

            File.WriteAllLines(ConfigPath, lines);
        }
    }

    /// <summary>
    /// 读取配置值，如果不存在则创建默认值并写入
    /// </summary>
    public static string GetValue(string key, string defaultValue = "")
    {
        if (_settings.TryGetValue(key, out var value))
            return value;

        // ✅ 3. 自动写入默认值
        SetValue(key, defaultValue);
        return defaultValue;
    }

    /// <summary>
    /// 设置配置值，如果不存在则添加
    /// </summary>
    public static void SetValue(string key, string value)
    {
        lock (_lock)
        {
            _settings[key] = value;
            Save();
        }
    }

    #endregion
}

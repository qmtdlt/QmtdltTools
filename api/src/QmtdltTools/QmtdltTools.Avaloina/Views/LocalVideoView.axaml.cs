using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;
using QmtdltTools.Avaloina.Dto;
using QmtdltTools.Avaloina.Utils;
using Serilog;
using Volo.Abp.DependencyInjection;
using Avalonia;

namespace QmtdltTools.Avaloina.Views;

public partial class LocalVideoView : UserControl, ITransientDependency
{
    private bool _isDragging = false;
    private DispatcherTimer _timer;
    private bool _isRepeating = false; // 是否正在单句重复
    private int _repeatIndex = -1;     // 当前单句重复的字幕 index
    private int _lastSubtitleIndex = -1;
    private LibVLC _libVLC;
    private MediaPlayer _mediaPlayer;
    Action<string> _updatingSubTitle;
    Action<string> _setSubTitle;
    List<SubtitleItem> subtitles = new List<SubtitleItem>();
    public LocalVideoView()
    {
        InitializeComponent();
        VolumeSlider.Value = 100; // 默认最大音量
        ProgressSlider.Minimum = 0;
        ProgressSlider.Maximum = 100;

        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(500);
        _timer.Tick += Timer_Tick;
        Loaded += LocalVideoView_Loaded;

        ProgressSlider.AddHandler(PointerPressedEvent, (s, e) =>
        {
            _isDragging = true;
        }, RoutingStrategies.Tunnel);
        ProgressSlider.AddHandler(PointerReleasedEvent, (s, e) =>
        {
            _isDragging = false;
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Time = (long)ProgressSlider.Value;
            }
        }, RoutingStrategies.Tunnel);
    }

    private void LocalVideoView_Loaded(object sender, RoutedEventArgs e)
    {
        LoadAppsettingProgress();
    }

    private void PrevSubtitleBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (_mediaPlayer == null || subtitles.Count == 0) return;

        var currentTime = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
        int currIdx = subtitles.FindIndex(s => currentTime >= s.Start && currentTime <= s.End);

        // 如果未命中，currIdx = -1，此时找第一个大于当前时间的字幕，回退一位
        if (currIdx == -1)
            currIdx = subtitles.FindIndex(s => currentTime < s.Start);
        if (currIdx == -1)
            currIdx = subtitles.Count - 1; // 当前时间大于所有字幕，取最后一句
        int prevIdx = Math.Max(currIdx - 1, 0);

        var prevSub = subtitles[prevIdx];
        _mediaPlayer.Time = (long)prevSub.Start.TotalMilliseconds;
        _lastSubtitleIndex = prevSub.Index;

        updateSubtitle();
    }

    private void NextSubtitleBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (_mediaPlayer == null || subtitles.Count == 0) return;

        var currentTime = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
        int currIdx = subtitles.FindIndex(s => currentTime >= s.Start && currentTime <= s.End);
        if (currIdx == -1)
            currIdx = subtitles.FindIndex(s => currentTime < s.Start);
        if (currIdx == -1)
            currIdx = subtitles.Count - 1;

        int nextIdx = Math.Min(currIdx + 1, subtitles.Count - 1);

        var nextSub = subtitles[nextIdx];
        _mediaPlayer.Time = (long)nextSub.Start.TotalMilliseconds;
        _lastSubtitleIndex = nextSub.Index;

        updateSubtitle();
    }
    private void RepeatBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (!_isRepeating)
        {
            _isRepeating = true;
            _repeatIndex = _lastSubtitleIndex;
            RepeatBtn.Content = "❌";
        }
        else
        {
            _isRepeating = false;
            _repeatIndex = -1;
            RepeatBtn.Content = "🔁";
        }
    }

    private void PauseBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (PauseBtn.Content.ToString() == "▶")
        {
            PauseBtn.Content = "⏸";
        }
        else
        {
            PauseBtn.Content = "▶";
        }
        _mediaPlayer?.Pause();
    }

    public void PauseMedia()
    {
        PauseBtn.Focus();
    }

    private void VolumeSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_mediaPlayer != null)
            _mediaPlayer.Volume = (int)VolumeSlider.Value;
    }
    
    private void Timer_Tick(object sender, EventArgs e)
    {
        try
        {
            if (_mediaPlayer == null || _mediaPlayer.Length <= 0) return;

            if (!_isDragging)
            {
                ProgressSlider.Maximum = _mediaPlayer.Length;
                ProgressSlider.Value = _mediaPlayer.Time;
            }
            TimeText.Text = $"{TimeSpan.FromMilliseconds(_mediaPlayer.Time):hh\\:mm\\:ss}/{TimeSpan.FromMilliseconds(_mediaPlayer.Length):hh\\:mm\\:ss}";

            updateSubtitle();
            // 单句循环逻辑
            if (_isRepeating && _repeatIndex != -1)
            {
                var sub = subtitles.FirstOrDefault(s => s.Index == _repeatIndex);
                var nextSub = subtitles.FirstOrDefault(s => s.Index == _repeatIndex + 1);

                if (sub != null && nextSub != null && _mediaPlayer != null)
                {
                    // 当前时间超过下句开头就回到本句开头
                    if (_mediaPlayer.Time >= nextSub.Start.TotalMilliseconds)
                    {
                        _mediaPlayer.Time = (long)sub.Start.TotalMilliseconds;
                    }
                }
                else if (sub != null && _mediaPlayer != null && sub.End != TimeSpan.Zero)
                {
                    // 没有下一句的情况（最后一句）
                    if (_mediaPlayer.Time >= sub.End.TotalMilliseconds)
                    {
                        _mediaPlayer.Time = (long)sub.Start.TotalMilliseconds;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
    void updateSubtitle()
    {
        var position = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
        var current = subtitles.FirstOrDefault(s => position >= s.Start && position <= s.End);
        if (current != null)
        {
            if (current.Index != _lastSubtitleIndex)
            {
                _setSubTitle?.Invoke(current.Text);
                _updatingSubTitle?.Invoke(current.Text);
                _lastSubtitleIndex = current.Index;
            }
        }
        else
        {
            if (_lastSubtitleIndex != -1)
            {
                _setSubTitle?.Invoke("");  // 清空字幕
                _lastSubtitleIndex = -1;
            }
        }
    }
    private void ProgressSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_mediaPlayer != null && _isDragging)
        {
            _mediaPlayer.Time = (long)ProgressSlider.Value;
        }
    }
    
    private async void OpenVideo(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择视频文件",
            Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "视频文件", Extensions = new List<string> { "mp4", "avi", "mkv", "flv", "mov", "wmv" } },
                    new FileDialogFilter { Name = "所有文件", Extensions = new List<string> { "*" } }
                },
            AllowMultiple = false
        };

        var window = this.GetVisualRoot() as Window;
        var result = await dialog.ShowAsync(window);
        if (result != null && result.Length > 0)
        {
            targetVideoPath.Text = result[0];
            loadVideo();
        }
    }
    void loadVideo()
    {
        // 构造字幕文件路径（同名、同目录）
        string subtitlePath = Path.ChangeExtension(targetVideoPath.Text, "英文.srt");
        //Sleepless.in.Seattle.1993.1080p.BluRay.X264-AMIABLE
        //Sleepless.in.Seattle.1993.1080p.BluRay.X264-AMIABLE.英文
        //subtitlePath = "Sleepless.in.Seattle.1993.1080p.BluRay.X264-AMIABLE.英文.srt";
        Debug.WriteLine($"找到字幕了：{subtitlePath}");
        bool subtitleExists = File.Exists(subtitlePath);

        string libvlcPath = null;
        string[] libvlcOptions = new[] { "--no-xlib" }; // 默认参数

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            libvlcPath = Path.Combine(AppContext.BaseDirectory, "libvlc", "osx-x64", "lib");
            Core.Initialize(libvlcPath);

            libvlcOptions = new[]
            {
                "--no-xlib",
                "--vout=macosx",
                "--avcodec-hw=none",
                "--no-sub-autodetect-file",
                "--sub-track=-1",
                "-vvv"
            };
             // 初始化 LibVLC
             _libVLC = new LibVLC(libvlcOptions);
            //_libVLC = new LibVLC();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // 初始化 LibVLC
            _libVLC = new LibVLC();
        }

        if (subtitleExists)
        {
            ParseSrt(subtitlePath);
        }

        _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);

        if (_mediaPlayer != null)
        {
            VideoView.MediaPlayer = _mediaPlayer;
            _mediaPlayer.Play(new Media(_libVLC, new Uri(targetVideoPath.Text)));
            // 设置延迟禁用字幕
            DispatcherTimer onceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            onceTimer.Tick += (s, e) =>
            {
                _mediaPlayer.SetSpu(-1);
                onceTimer.Stop();
            };
            onceTimer.Start();
            _timer.Start();
        }
    }
    void LoadAppsettingProgress()
    {
        if (File.Exists(AppSettingHelper.LastVideoPath))
        {
            targetVideoPath.Text = AppSettingHelper.LastVideoPath;
            loadVideo();
            // 定位进度
            try
            {
                if (AppSettingHelper.LastVideoProgress > 0)
                {
                    _mediaPlayer.Time = AppSettingHelper.LastVideoProgress;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }
    }
    internal void onClose()
    {
        try
        {
            AppSettingHelper.LastVideoPath = targetVideoPath.Text;
            AppSettingHelper.LastVideoProgress = _mediaPlayer.Time;
            _timer.Stop();
            // 释放资源
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
            _mediaPlayer = null;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
    }
    internal void InitAction(Action<string> updatingSubTitle, Action<string> setSubTitle1)
    {
        _updatingSubTitle = updatingSubTitle;
        _setSubTitle = setSubTitle1;
    }

    public List<SubtitleItem> ParseSrt(string filePath)
    {
        var lines = System.IO.File.ReadAllLines(filePath);
        int i = 0;
        while (i < lines.Length)
        {
            if (int.TryParse(lines[i], out int index))
            {
                var times = lines[i + 1].Split(new string[] { " --> " }, StringSplitOptions.None);
                var start = TimeSpan.Parse(times[0].Replace(',', '.'));
                var end = TimeSpan.Parse(times[1].Replace(',', '.'));
                var text = "";
                int j = i + 2;
                while (j < lines.Length && !string.IsNullOrWhiteSpace(lines[j]))
                {
                    text += lines[j] + Environment.NewLine;
                    j++;
                }
                subtitles.Add(new SubtitleItem
                {
                    Index = index,
                    Start = start,
                    End = end,
                    Text = text.Trim()
                });
                i = j;
            }
            else
            {
                i++;
            }
        }
        return subtitles;
    }

    internal void GoLastSentence()
    {
        PrevSubtitleBtn_Click(null, null);
    }

    internal void GoNextSentence()
    {
        NextSubtitleBtn_Click(null, null);
    }

    internal void RepeatOne()
    {
        if (!_isRepeating)
        {
            _isRepeating = true;
            _repeatIndex = _lastSubtitleIndex;
            RepeatBtn.Content = "❌";
        }
        else
        {
            _isRepeating = false;
            _repeatIndex = -1;
            RepeatBtn.Content = "🔁";
        }
    }

    internal void CancelRepeat()
    {
        if (!_isRepeating)
        {
            _isRepeating = true;
            _repeatIndex = _lastSubtitleIndex;
            RepeatBtn.Content = "❌";
        }
        else
        {
            _isRepeating = false;
            _repeatIndex = -1;
            RepeatBtn.Content = "🔁";
        }
    }
}

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
    private readonly DispatcherTimer _timer;

    private bool _isRepeating = false;
    private int _repeatIndex = -1;
    private int _lastSubtitleIndex = -1;

    private LibVLC _libVLC;
    private MediaPlayer _mediaPlayer;
    private Media _currentMedia;                 // ★ 新增：持有当前 Media

    private Action<string> _updatingSubTitle;
    private Action<string> _setSubTitle;
    private readonly List<SubtitleItem> subtitles = new();

    public LocalVideoView()
    {
        InitializeComponent();

        VolumeSlider.Value = 100;
        ProgressSlider.Minimum = 0;
        ProgressSlider.Maximum = 100;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += Timer_Tick;

        Loaded += LocalVideoView_Loaded;

        // 拖动滑块时只在释放时 Seek
        ProgressSlider.AddHandler(PointerPressedEvent, (s, e) => _isDragging = true, RoutingStrategies.Tunnel);
        ProgressSlider.AddHandler(PointerReleasedEvent, (s, e) =>
        {
            _isDragging = false;
            if (_mediaPlayer != null)
                _mediaPlayer.Time = (long)ProgressSlider.Value;
        }, RoutingStrategies.Tunnel);
    }

    private void LocalVideoView_Loaded(object sender, RoutedEventArgs e)
    {
        InitVlcOnce();
        LoadAppsettingProgress();
    }

    // ★ 只初始化一次 LibVLC & MediaPlayer，并绑定到 VideoView
    private void InitVlcOnce()
    {
        if (_libVLC != null) return;

        Core.Initialize(); // 不手动指定 mac 的 vout，避免独立窗口

        var options = new[]：q
        {
            "--no-video-title-show",
            "--no-osd",
            "--no-sub-autodetect-file",
            "--sub-track=-1" // 禁用 VLC 内建字幕（你自己做字幕）
        };

        _libVLC = new LibVLC(options);
        _mediaPlayer = new MediaPlayer(_libVLC);

        // 关键：只设置一次
        VideoView.MediaPlayer = _mediaPlayer;

        _timer.Start();
    }

    // ★ 统一的播放入口：切换文件时不会弹外部窗口
    private void PlayFile(string path, string subtitlePath = null)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        // 停止旧媒体
        try { _mediaPlayer?.Stop(); } catch { /* ignore */ }

        // 释放旧 Media
        _currentMedia?.Dispose();
        _currentMedia = null;

        // 解析外挂字幕（你自己的显示）
        subtitles.Clear();
        if (!string.IsNullOrEmpty(subtitlePath) && File.Exists(subtitlePath))
            ParseSrt(subtitlePath);

        // 创建并持有新 Media（不要 using 立刻释放）
        _currentMedia = new Media(_libVLC, path, FromType.FromPath);

        // 禁用 VLC 自带字幕
        _mediaPlayer.SetSpu(-1);

        // 播放
        _mediaPlayer.Play(_currentMedia);

        // UI 状态同步
        PauseBtn.Content = "⏸";
        _isRepeating = false;
        _repeatIndex = -1;
        RepeatBtn.Content = "🔁";
    }

    private void PrevSubtitleBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (_mediaPlayer == null || subtitles.Count == 0) return;

        var currentTime = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
        int currIdx = subtitles.FindIndex(s => currentTime >= s.Start && currentTime <= s.End);

        if (currIdx == -1)
            currIdx = subtitles.FindIndex(s => currentTime < s.Start);
        if (currIdx == -1)
            currIdx = subtitles.Count - 1;

        int prevIdx = Math.Max(currIdx - 1, 0);

        var prevSub = subtitles[prevIdx];
        _mediaPlayer.Time = (long)prevSub.Start.TotalMilliseconds;
        _lastSubtitleIndex = prevSub.Index;

        updateSubtitle();
        PauseBtn.Focus();
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
        PauseBtn.Focus();
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
        PauseBtn.Focus();
    }

    private void PauseBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (PauseBtn.Content?.ToString() == "▶")
            PauseBtn.Content = "⏸";
        else
            PauseBtn.Content = "▶";

        _mediaPlayer?.Pause();
    }

    public void PauseMedia() => PauseBtn.Focus();

    private void VolumeSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_mediaPlayer != null)
            _mediaPlayer.Volume = (int)VolumeSlider.Value;
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        try
        {
            if (_mediaPlayer == null || _mediaPlayer.Length <= 0) return;

            if (!_isDragging)
            {
                ProgressSlider.Maximum = _mediaPlayer.Length;
                ProgressSlider.Value = _mediaPlayer.Time;
            }

            TimeText.Text =
                $"{TimeSpan.FromMilliseconds(_mediaPlayer.Time):hh\\:mm\\:ss}/{TimeSpan.FromMilliseconds(_mediaPlayer.Length):hh\\:mm\\:ss}";

            updateSubtitle();

            // 单句循环
            if (_isRepeating && _repeatIndex != -1)
            {
                var sub = subtitles.FirstOrDefault(s => s.Index == _repeatIndex);
                var nextSub = subtitles.FirstOrDefault(s => s.Index == _repeatIndex + 1);

                if (sub != null && nextSub != null)
                {
                    if (_mediaPlayer.Time >= nextSub.Start.TotalMilliseconds)
                        _mediaPlayer.Time = (long)sub.Start.TotalMilliseconds;
                }
                else if (sub != null && sub.End != TimeSpan.Zero)
                {
                    if (_mediaPlayer.Time >= sub.End.TotalMilliseconds)
                        _mediaPlayer.Time = (long)sub.Start.TotalMilliseconds;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private void updateSubtitle()
    {
        if (_mediaPlayer == null) return;

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
                _setSubTitle?.Invoke("");
                _lastSubtitleIndex = -1;
            }
        }
    }

    private void ProgressSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_mediaPlayer != null && _isDragging)
            _mediaPlayer.Time = (long)ProgressSlider.Value;
    }

    private async void OpenVideo(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择视频文件",
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter { Name = "视频文件", Extensions = new() { "mp4","avi","mkv","flv","mov","wmv" } },
                new FileDialogFilter { Name = "所有文件", Extensions = new() { "*" } }
            },
            AllowMultiple = false
        };

        var window = this.GetVisualRoot() as Window;
        var result = await dialog.ShowAsync(window);
        if (result is { Length: > 0 })
        {
            var videoPath = result[0];
            targetVideoPath.Text = videoPath;

            // 你项目里的配置：可用同名SRT或 AppSettingHelper.LastVideoSrt
            string subtitlePath = AppSettingHelper.LastVideoSrt;

            PlayFile(videoPath, subtitlePath);
        }
    }

    private async void SelectMatchSubTitle(object? sender, RoutedEventArgs e)
    {
        var wd = App.Get<ChooseSubtitle>();
        if (wd != null && !string.IsNullOrEmpty(targetVideoPath.Text) && File.Exists(targetVideoPath.Text))
        {
            wd.SetMoviePath(targetVideoPath.Text);
            wd.Show();
        }
    }

    private async void OpenSetting(object? sender, RoutedEventArgs e)
    {
        var wd = App.Get<SysSetting>();
        if (wd != null)
            wd.Show();
    }

    private void LoadAppsettingProgress()
    {
        try
        {
            if (File.Exists(AppSettingHelper.LastVideoPath))
            {
                targetVideoPath.Text = AppSettingHelper.LastVideoPath;
                PlayFile(targetVideoPath.Text, AppSettingHelper.LastVideoSrt);

                if (AppSettingHelper.LastVideoProgress > 0)
                    _mediaPlayer.Time = AppSettingHelper.LastVideoProgress;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "恢复进度失败");
        }
    }

    internal void onClose()
    {
        try
        {
            AppSettingHelper.LastVideoPath = targetVideoPath.Text;
            if (_mediaPlayer != null)
                AppSettingHelper.LastVideoProgress = _mediaPlayer.Time;

            _timer.Stop();
            _mediaPlayer?.Stop();

            _currentMedia?.Dispose();
            _currentMedia = null;

            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
            _mediaPlayer = null;
            _libVLC = null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "关闭释放失败");
        }
    }

    internal void InitAction(Action<string> updatingSubTitle, Action<string> setSubTitle1)
    {
        _updatingSubTitle = updatingSubTitle;
        _setSubTitle = setSubTitle1;
    }

    public List<SubtitleItem> ParseSrt(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        int i = 0;
        while (i < lines.Length)
        {
            if (int.TryParse(lines[i], out int index))
            {
                var times = lines[i + 1].Split(new[] { " --> " }, StringSplitOptions.None);
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
                    Text = text.Trim().Replace("<i>", "").Replace("</i>", "")
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

    internal void GoLastSentence() => PrevSubtitleBtn_Click(null, null);
    internal void GoNextSentence() => NextSubtitleBtn_Click(null, null);

    internal void RepeatOne()
    {
        _isRepeating = !_isRepeating;
        _repeatIndex = _isRepeating ? _lastSubtitleIndex : -1;
        RepeatBtn.Content = _isRepeating ? "❌" : "🔁";
    }

    internal void CancelRepeat()
    {
        _isRepeating = false;
        _repeatIndex = -1;
        RepeatBtn.Content = "🔁";
    }
}

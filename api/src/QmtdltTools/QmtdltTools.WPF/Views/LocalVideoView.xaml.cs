using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using QmtdltTools.WPF.Utils;
using Serilog;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.WPF.Views
{
    public partial class LocalVideoView : UserControl,ITransientDependency
    {
        private bool _isDragging = false;
        private DispatcherTimer _timer;
        private bool _isRepeating = false; // 是否正在单句重复
        private int _repeatIndex = -1;     // 当前单句重复的字幕 index
        public LocalVideoView()
        {
            InitializeComponent();

            ProgressSlider.AddHandler(
                System.Windows.Controls.Primitives.Thumb.DragStartedEvent,
                new System.Windows.Controls.Primitives.DragStartedEventHandler(ProgressSlider_DragStarted));
            ProgressSlider.AddHandler(
                System.Windows.Controls.Primitives.Thumb.DragCompletedEvent,
                new System.Windows.Controls.Primitives.DragCompletedEventHandler(ProgressSlider_DragCompleted));

            VolumeSlider.Value = 100; // 默认最大音量
            ProgressSlider.Minimum = 0;
            ProgressSlider.Maximum = 100;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += Timer_Tick;
            Loaded += LocalVideoView_Loaded;
        }

        private void LocalVideoView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAppsettingProgress();
        }

        private void PrevSubtitleBtn_Click(object sender, RoutedEventArgs e)
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

        private void NextSubtitleBtn_Click(object sender, RoutedEventArgs e)
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
        private void RepeatBtn_Click(object sender, RoutedEventArgs e)
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

        private void PauseBtn_Click(object sender, RoutedEventArgs e)
        {
            if(PauseBtn.Content.ToString() == "▶")
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

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mediaPlayer != null)
                _mediaPlayer.Volume = (int)VolumeSlider.Value;
        }
        private int _lastSubtitleIndex = -1;
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer == null || _mediaPlayer.Length <= 0) return;

            if (!_isDragging)
            {
                ProgressSlider.Maximum = _mediaPlayer.Length;
                ProgressSlider.Value = _mediaPlayer.Time;
            }
            TimeText.Text = $"{TimeSpan.FromMilliseconds(_mediaPlayer.Time):mm\\:ss}/{TimeSpan.FromMilliseconds(_mediaPlayer.Length):mm\\:ss}";

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
        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mediaPlayer != null && _isDragging)
            {
                _mediaPlayer.Time = (long)ProgressSlider.Value;
            }
        }

        private void ProgressSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _isDragging = true;
        }

        private void ProgressSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            _isDragging = false;
            if (_mediaPlayer != null)
                _mediaPlayer.Time = (long)ProgressSlider.Value;
        }

        private LibVLC _libVLC;
        private LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
        private void OpenVideo(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = "视频文件|*.mp4;*.avi;*.mkv;*.flv;*.mov;*.wmv|所有文件|*.*";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                targetVideoPath.Text = dialog.FileName;
                loadVideo();
            }
        }
        void loadVideo()
        {
            // 构造字幕文件路径（同名、同目录）
            string subtitlePath = System.IO.Path.ChangeExtension(targetVideoPath.Text, "英文.srt");
            bool subtitleExists = System.IO.File.Exists(subtitlePath);

            _libVLC = new LibVLC();
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
            VideoView.MediaPlayer = _mediaPlayer; // 假设 VideoView 是 XAML 中的控件
            _mediaPlayer.Play(new Media(_libVLC, new Uri(targetVideoPath.Text)));

            if (subtitleExists)
            {
                ParseSrt(subtitlePath);
            }
            if (_mediaPlayer != null)
            {
                VideoView.MediaPlayer = _mediaPlayer;
                _mediaPlayer.Play(new Media(_libVLC, new Uri(targetVideoPath.Text)));
                _timer.Start();
            }
        }
        void LoadAppsettingProgress()
        {
            if(File.Exists(AppSettingHelper.LastVideoPath))
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
        Action<string> _updatingSubTitle;
        Action<string> _setSubTitle;
        internal void InitAction(Action<string> updatingSubTitle, Action<string> setSubTitle1)
        {
            _updatingSubTitle = updatingSubTitle;
            _setSubTitle = setSubTitle1;
        }

        List<SubtitleItem> subtitles = new List<SubtitleItem>();
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

    public class SubtitleItem
    {
        public int Index { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public string Text { get; set; }
    }
}
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using Serilog;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using IVideoView = QmtdltTools.WPF.IServices.IVideoView;

namespace QmtdltTools.WPF.Views
{
    public partial class LocalVideoView : UserControl, IVideoView
    {
        private bool _isDragging = false;
        private DispatcherTimer _timer;
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
        }
        private void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            _mediaPlayer?.Play();
            _timer.Start();
        }

        private void PauseBtn_Click(object sender, RoutedEventArgs e)
        {
            _mediaPlayer?.Pause();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mediaPlayer != null)
                _mediaPlayer.Volume = (int)VolumeSlider.Value;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer == null || _mediaPlayer.Length <= 0) return;

            if (!_isDragging)
            {
                ProgressSlider.Maximum = _mediaPlayer.Length;
                ProgressSlider.Value = _mediaPlayer.Time;
            }
            TimeText.Text = $"{TimeSpan.FromMilliseconds(_mediaPlayer.Time):mm\\:ss}/{TimeSpan.FromMilliseconds(_mediaPlayer.Length):mm\\:ss}";
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
                _libVLC = new LibVLC();
                _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
                VideoView.MediaPlayer = _mediaPlayer; // 假设 VideoView 是 XAML 中的控件
                _mediaPlayer.Play(new Media(_libVLC, new Uri(dialog.FileName)));
            }
            if (_mediaPlayer != null)
            {
                VideoView.MediaPlayer = _mediaPlayer;
                _mediaPlayer.Play(new Media(_libVLC, new Uri(dialog.FileName)));
                _timer.Start();

                //ProgressSlider.AddHandler(Slider.DragStartedEvent, new System.Windows.Controls.Primitives.DragStartedEventHandler(ProgressSlider_DragStarted));
                //ProgressSlider.AddHandler(Slider.DragCompletedEvent, new System.Windows.Controls.Primitives.DragCompletedEventHandler(ProgressSlider_DragCompleted));
            }
        }

        internal void onClose()
        {
            try
            {
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
    }
}
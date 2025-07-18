﻿using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using NAudio.Wave;
using QmtdltTools.Domain.Enums;
using QmtdltTools.WPF.IServices;
using QmtdltTools.WPF.Services;
using ScottPlot;
using ScottPlot.Plottables;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.WPF.Views
{
    /// <summary>
    /// PlayGround.xaml 的交互逻辑
    /// </summary>
    public partial class PlayGround : Window, ITransientDependency
    {
        private readonly TransService _transService;
        public PlayGround(PlayGroundVm vm, TransService transService)
        {
            InitializeComponent();
            _transService = transService;
            DataContext = vm;
            this.Closing += MainWindow_Closing;
        }
        public void SetType(VideoCollectionType videoCollectionType)
        {
            if (DataContext is PlayGroundVm vm)
            {
                vm.SetType(videoCollectionType);
            }
        }
        private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb)
            {
                string selectedText = tb.SelectedText;
                if (!string.IsNullOrEmpty(selectedText))
                {
                    _ = _transService.Trans(selectedText);
                }
            }
        }
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is PlayGroundVm vm)
            {
                vm.onClose();
            }
        }

        internal void LoadUrl(string videoUrl)
        {
            if (DataContext is PlayGroundVm vm)
            {
                vm.LoadUrl(videoUrl);
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (DataContext is PlayGroundVm vm)
                {
                    vm.PauseVideo();
                }
            }
            if (e.Key == Key.Left)
            {
                if (DataContext is PlayGroundVm vm)
                {
                    vm.GoLastSentence();
                }
            }
            if (e.Key == Key.Right)
            {
                if (DataContext is PlayGroundVm vm)
                {
                    vm.GoNextSentence();
                }
            }
            if (e.Key == Key.Up)
            {
                if (DataContext is PlayGroundVm vm)
                {
                    vm.RepeatOne();
                }
            }
            if (e.Key == Key.Down)
            {
                if (DataContext is PlayGroundVm vm)
                {
                    vm.CancelRepeat();
                }
            }
        }

        private void TextBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb)
            {
                string selectedText = tb.SelectedText;
                if (!string.IsNullOrEmpty(selectedText))
                {
                    _ = _transService.Trans(selectedText);
                }
            }
        }
    }

    public class PlayGroundVm : BindableBase, ITransientDependency
    {
        const string startRecord = "开始录音";
        const string stopRecord = "停止录音";
        private WaveInEvent waveIn;
        private MemoryStream audioStream;
        private WaveFileWriter waveFileWriter;
        private bool isRecording = false;
        string tempAudioFilePath = "";
        VideoCollectionType _videoCollectionType;
        ConcurrentQueue<string> subtitleQueue = new ConcurrentQueue<string>();          // 字幕队列

        ISubtitleService _subtitleService;
        public DelegateCommand AudioRecordCmd { get; set; }
        public DelegateCommand CheckShadowingCmd { get; set; }
        public PlayGroundVm(ISubtitleService subtitleService)
        {
            _subtitleService = subtitleService;

            CheckShadowingCmd = new DelegateCommand(checkShadowing);

            AudioRecordCmd = new DelegateCommand(onRecordClick);
            RecordBtnContent = startRecord;
        }

        private async void checkShadowing()
        {
            try
            {
                if (File.Exists(tempAudioFilePath))
                {
                    StatusText = "正在进行评估...";
                    PronunciationResult = null;

                    OverAllPlot = null;

                    PronunciationResult = await MsTTSHelperRest.PronunciationAssessmentWithLocalWavFileAsync(tempAudioFilePath, CurSubtitle);

                    if(PronunciationResult != null)
                    {
                        var view = App.Get<PronunciationEvaluation>();
                        view.SetScores(PronunciationResult);
                        OverAllPlot = view;
                        StatusText = "请查看发音评价。";
                    }
                    else
                    {
                        MessageBox.Show("未获取到评价，请重试");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void onRecordClick()
        {
            if (!isRecording)
            {
                StartRecording();           // 录音
            }
            else
            {
                StopRecording();            // 停止录音
            }
        }
        private void StartRecording()
        {
            try
            {
                waveIn = new WaveInEvent();
                waveIn.WaveFormat = new WaveFormat(16000, 1); // 16kHz, mono
                audioStream = new MemoryStream();
                waveFileWriter = new WaveFileWriter(audioStream, waveIn.WaveFormat);

                waveIn.DataAvailable += (s, a) =>
                {
                    waveFileWriter.Write(a.Buffer, 0, a.BytesRecorded);
                };

                waveIn.StartRecording();
                isRecording = true;
                RecordBtnContent = stopRecord;
                IsSubmitEnable = false;
                StatusText = "正在录音...";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"录音启动失败: {ex.Message}");
            }
        }
        private void StopRecording()
        {
            try
            {
                waveIn.StopRecording();
                waveFileWriter.Flush();
                isRecording = false;
                RecordBtnContent = startRecord;
                IsSubmitEnable = true;
                StatusText = "录音已停止，可以提交或播放";

                // Save audio to a temporary file
                tempAudioFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"recording_submit.wav");
                File.WriteAllBytes(tempAudioFilePath, audioStream.ToArray());

                // Prepare audio for playback
                RecordAudioUri = new Uri(tempAudioFilePath, UriKind.Absolute);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"录音停止失败: {ex.Message}");
            }
        }
        public void onClose()
        {
            if (VideoView is WebVideoView view1)
            {
                view1.onClose();
            }
            if (VideoView is LocalVideoView view2)
            {
                view2.onClose();
            }
        }

        void updatingTitle(string subTitle)
        {
            CurSubtitle = subTitle;
        }
        void SetSubTitle(string subTitle)
        {
            subtitleQueue.Enqueue(subTitle);
            updatePastSubtitles();
        }
        void updatePastSubtitles()
        {
            if (subtitleQueue.Count > 4)
            {
                subtitleQueue.TryDequeue(out string? data);
            }
            var list = subtitleQueue.ToList();
            if (list.Count > 1)
            {
                if (_videoCollectionType == VideoCollectionType.OffLine)
                {
                    PastSubtitle = string.Join("\n", list.Take(list.Count - 1));
                }
                else
                {
                    PastSubtitle = string.Join("\n", list);
                }
            }
        }

        public void LoadUrl(string url)
        {
            if (VideoView is WebVideoView view)
            {
                view.LoadUrl(url);
            }
        }
        internal void SetType(VideoCollectionType videoCollectionType)
        {
            _videoCollectionType = videoCollectionType;
            if (videoCollectionType == VideoCollectionType.OnLine)
            {
                _ = Task.Run(async () =>
                {
                    await _subtitleService.StopAsync();
                    await _subtitleService.StartRecognizeAsync(updatingTitle, SetSubTitle);
                });
                VideoView = App.Get<WebVideoView>();
            }
            else
            {
                var view = App.Get<LocalVideoView>();
                view.InitAction(updatingTitle, SetSubTitle);
                VideoView = view;
            }
        }

        internal void PauseVideo()
        {
            if (_videoCollectionType == VideoCollectionType.OnLine)
            {
                if (VideoView is WebVideoView view1)
                {
                    //view1.pause();
                }
            }
            else
            {
                if (VideoView is LocalVideoView view2)
                {
                    view2.PauseMedia();
                }
            }
        }

        internal void GoLastSentence()
        {
            if (_videoCollectionType == VideoCollectionType.OnLine)
            {
                if (VideoView is WebVideoView view1)
                {
                    //view1.pause();
                }
            }
            else
            {
                if (VideoView is LocalVideoView view2)
                {
                    view2.GoLastSentence();
                }
            }
        }

        internal void GoNextSentence()
        {
            if (_videoCollectionType == VideoCollectionType.OnLine)
            {
                if (VideoView is WebVideoView view1)
                {
                    //view1.pause();
                }
            }
            else
            {
                if (VideoView is LocalVideoView view2)
                {
                    view2.GoNextSentence();
                }
            }
        }

        internal void RepeatOne()
        {
            if (_videoCollectionType == VideoCollectionType.OnLine)
            {
                if (VideoView is WebVideoView view1)
                {
                    //view1.pause();
                }
            }
            else
            {
                if (VideoView is LocalVideoView view2)
                {
                    view2.RepeatOne();
                }
            }
        }

        internal void CancelRepeat()
        {
            if (_videoCollectionType == VideoCollectionType.OnLine)
            {
                if (VideoView is WebVideoView view1)
                {
                    //view1.pause();
                }
            }
            else
            {
                if (VideoView is LocalVideoView view2)
                {
                    view2.CancelRepeat();
                }
            }
        }

        private Uri recordAudioUri;
        public Uri RecordAudioUri
        {
            get { return recordAudioUri; }
            set
            {
                recordAudioUri = value;
                this.RaisePropertyChanged("RecordAudioUri");
            }
        }
        private bool isSubmitEnable;
        public bool IsSubmitEnable
        {
            get { return isSubmitEnable; }
            set
            {
                isSubmitEnable = value;
                this.RaisePropertyChanged("IsSubmitEnable");
            }
        }
        private string statusText;
        public string StatusText
        {
            get { return statusText; }
            set
            {
                statusText = value;
                this.RaisePropertyChanged("StatusText");
            }
        }
        private string recordBtnContent;
        public string RecordBtnContent
        {
            get { return recordBtnContent; }
            set
            {
                recordBtnContent = value;
                this.RaisePropertyChanged("RecordBtnContent");
            }
        }
        private string curSubtitle;
        public string CurSubtitle
        {
            get { return curSubtitle; }
            set
            {
                curSubtitle = value;
                this.RaisePropertyChanged("CurSubtitle");
            }
        }
        private string pastSubtitle;
        public string PastSubtitle
        {
            get { return pastSubtitle; }
            set
            {
                pastSubtitle = value;
                this.RaisePropertyChanged("PastSubtitle");
            }
        }
        private IVideoView videoView;
        public IVideoView VideoView
        {
            get { return videoView; }
            set
            {
                videoView = value;
                this.RaisePropertyChanged("VideoView");
            }
        }
        private PronunciationAssessmentResult? pronunciationResult;
        public PronunciationAssessmentResult? PronunciationResult
        {
            get { return pronunciationResult; }
            set
            {
                pronunciationResult = value;
                this.RaisePropertyChanged("PronunciationResult");
            }
        }

        private object overAllPlot;
        public object OverAllPlot
        {
            get { return overAllPlot; }
            set
            {
                overAllPlot = value;
                this.RaisePropertyChanged("OverAllPlot");
            }
        }
    }
}

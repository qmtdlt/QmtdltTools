using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using QmtdltTools.Avaloina.Services;
using QmtdltTools.Avaloina.ViewModels;
using ReactiveUI;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Avaloina.Views;

public partial class PlayGround : Window,ITransientDependency
{
    private readonly TransRestService _transService;
    public PlayGround(PlayGroundVm vm, TransRestService transService)
    {
        InitializeComponent();
        _transService = transService;
        DataContext = vm;
    }

    private void TextBox_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
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

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is PlayGroundVm vm)
        {
            vm.onClose();
        }
    }
}

public class PlayGroundVm : ViewModelBase, ITransientDependency
{
    const string startRecord = "开始录音";
    const string stopRecord = "停止录音";
    // private WaveInEvent waveIn;
    //private MemoryStream audioStream;
    // private WaveFileWriter waveFileWriter;
    private bool isRecording = false;
    string tempAudioFilePath = "";
    ConcurrentQueue<string> subtitleQueue = new ConcurrentQueue<string>();          // 字幕队列

    public ReactiveCommand<Unit, Unit> AudioRecordCmd { get; set; }
    public ReactiveCommand<Unit, Unit> CheckShadowingCmd { get; set; }
    public PlayGroundVm()
    {
        CheckShadowingCmd = ReactiveCommand.Create(checkShadowing);

        AudioRecordCmd = ReactiveCommand.Create(onRecordClick);
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
                    // var view = App.Get<PronunciationEvaluation>();
                    // view.SetScores(PronunciationResult);
                    // OverAllPlot = view;
                    StatusText = "请查看发音评价。";
                }
                else
                {
                    // MessageBox.Show("未获取到评价，请重试");
                }
            }
        }
        catch (Exception ex)
        {
            // MessageBox.Show(ex.Message);
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
            //StopRecording();            // 停止录音
        }
    }
    private void StartRecording()
    {
        try
        {
            // waveIn = new WaveInEvent();
            // waveIn.WaveFormat = new WaveFormat(16000, 1); // 16kHz, mono
            // audioStream = new MemoryStream();
            // waveFileWriter = new WaveFileWriter(audioStream, waveIn.WaveFormat);
            //
            // waveIn.DataAvailable += (s, a) =>
            // {
            //     waveFileWriter.Write(a.Buffer, 0, a.BytesRecorded);
            // };
            //
            // waveIn.StartRecording();
            isRecording = true;
            RecordBtnContent = stopRecord;
            IsSubmitEnable = false;
            StatusText = "正在录音...";
        }
        catch (Exception ex)
        {
            // MessageBox.Show($"录音启动失败: {ex.Message}");
        }
    }
    private void StopRecording()
    {
        try
        {
            // waveIn.StopRecording();
            // waveFileWriter.Flush();
            //isRecording = false;
            //RecordBtnContent = startRecord;
            //IsSubmitEnable = true;
            //StatusText = "录音已停止，可以提交或播放";

            //// Save audio to a temporary file
            //tempAudioFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"recording_submit.wav");
            //File.WriteAllBytes(tempAudioFilePath, audioStream.ToArray());

            //// Prepare audio for playback
            //RecordAudioUri = new Uri(tempAudioFilePath, UriKind.Absolute);
        }
        catch (Exception ex)
        {
            // MessageBox.Show($"录音停止失败: {ex.Message}");
        }
    }
    public void onClose()
    {
        VideoView.onClose();
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
            PastSubtitle = string.Join("\n", list);
        }
    }


    internal void PauseVideo()
    {
        VideoView.PauseMedia();
    }

    internal void GoLastSentence()
    {
        VideoView.GoLastSentence();
    }

    internal void GoNextSentence()
    {
        VideoView.GoNextSentence();
    }

    internal void RepeatOne()
    {
        VideoView.RepeatOne();
    }

    internal void CancelRepeat()
    {
        VideoView.CancelRepeat();
    }

    public Uri RecordAudioUri { get; set; }
    public bool IsSubmitEnable { get; set; }
    public string StatusText { get; set; }
    public string RecordBtnContent { get; set; }
    public string CurSubtitle { get; set; }
    public string PastSubtitle { get; set; }
    public LocalVideoView VideoView { get; set; }
    public PronunciationAssessmentResult? PronunciationResult { get; set; }
    public object OverAllPlot { get; set; }
}
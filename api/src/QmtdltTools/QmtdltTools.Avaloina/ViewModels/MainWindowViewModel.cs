using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using QmtdltTools.Avaloina.Utils;
using QmtdltTools.Domain.Data;
using ReactiveUI;
using System.Collections.Concurrent;
using System.Reactive;
using System;
using Volo.Abp.DependencyInjection;
using QmtdltTools.Avaloina.Views;
using System.Linq;
using System.IO;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Structs;
using System.Windows.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using QmtdltTools.Avaloina.Dto;

namespace QmtdltTools.Avaloina.ViewModels;

public partial class MainWindowViewModel : ReactiveObject, ISingletonDependency
{
    private MiniAudioEngine? _audioEngine;
    private AudioCaptureDevice? _captureDevice;
    private Recorder? _recorder;
    private FileStream? _fileStream;
    private readonly string _outputFilePath;

    const string startRecord = "开始录音";
    const string stopRecord = "停止录音";
    private bool isRecording = false;

    public ICommand AudioRecordCmd { get; }
    public ICommand CheckShadowingCmd { get; set; }
    public MainWindowViewModel()
    {
        _outputFilePath = Path.Combine(Directory.GetCurrentDirectory(), "recording.wav");
        CheckShadowingCmd = ReactiveCommand.Create(checkShadowing);

        AudioRecordCmd = ReactiveCommand.Create(onRecordClick);
        RecordBtnContent = startRecord;
    }

    private async void checkShadowing()
    {
        try
        {
            if (File.Exists(_outputFilePath))
            {
                StatusText = "正在进行评估...";
                PronunciationResult = null;

                OverAllPlot = null;

                PronunciationResult = await RestHelper.CheckShadowing(_outputFilePath, CurSubTitle);

                if (PronunciationResult != null)
                {
                    var view = App.Get<PronunciationEvaluation>();
                    view.SetScores(PronunciationResult);
                    OverAllPlot = view;
                    StatusText = "请查看发音评价。";
                }
                else
                {
                    // MessageBox.Show("未获取到评价，请重试");
                    MessageBoxManager.GetMessageBoxStandard("提示", "未获取到评价，请重试", ButtonEnum.Ok).ShowAsync();
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
            StopRecording();            // 停止录音
        }
    }
    private void StartRecording()
    {
        // 如果正在錄音，則不執行任何操作
        if (_recorder is not null)
        {
            return;
        }

        try
        {
            _audioEngine = new MiniAudioEngine();

            // 尋找預設的擷取（錄音）設備
            var defaultCaptureDevice = _audioEngine.CaptureDevices.FirstOrDefault(d => d.IsDefault);
            if (defaultCaptureDevice.Id == IntPtr.Zero)
            {
                Console.WriteLine("找不到錄音設備！");
                return;
            }

            // 定義錄音的音訊格式
            var audioFormat = new AudioFormat
            {
                Format = SampleFormat.F32,
                SampleRate = 48000,
                Channels = 1 // 單聲道
            };

            // 初始化擷取設備
            _captureDevice = _audioEngine.InitializeCaptureDevice(defaultCaptureDevice, audioFormat);

            // 設定輸出檔案流
            _fileStream = new FileStream(_outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None);

            // 建立錄音器，將其與擷取設備和輸出流連結
            _recorder = new Recorder(_captureDevice, _fileStream, EncodingFormat.Wav);

            // 開始擷取和錄製
            _captureDevice.Start();
            _recorder.StartRecording();

            // 更新UI：禁用開始按鈕，啟用停止按鈕

            isRecording = true;
            RecordBtnContent = stopRecord;
            IsSubmitEnable = false;
            StatusText = "正在录音...";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"錄音失敗: {ex.Message}");
            // 如果出錯，清理資源
            StopRecording();
        }
    }
    private void StopRecording()
    {
        try
        {
            // 檢查是否有正在進行的錄音
            if (_recorder is null || _captureDevice is null)
            {
                return;
            }

            StatusText = "正在停止錄音...";

            // 依照順序停止和釋放資源
            _recorder.StopRecording();
            _captureDevice.Stop();

            // 呼叫 Dispose 來確保檔案寫入完成並釋放所有控制代碼
            _recorder.Dispose();
            _fileStream?.Dispose(); // Recorder Dispose 後，也建議 Dispose Stream
            _captureDevice.Dispose();
            _audioEngine?.Dispose();

            // 將欄位設為 null，表示錄音已結束，可進行下一次錄音
            _recorder = null;
            _fileStream = null;
            _captureDevice = null;
            _audioEngine = null;

            // 更新UI：啟用開始按鈕，禁用停止按鈕
            isRecording = false;
            RecordBtnContent = startRecord;
            IsSubmitEnable = true;
            StatusText = "录音已停止，可以提交或播放";
        }
        catch (Exception ex)
        {
            // MessageBox.Show($"录音停止失败: {ex.Message}");
        }
    }

    internal void updateCurSubtitle(string subTitle)
    {
        CurSubTitle = subTitle;
    }

    public string CurSubTitle { get; set; }
    public Uri RecordAudioUri { get; set; }
    //public bool IsSubmitEnable { get; set; }

    private bool _isSubmitEnable;
    public bool IsSubmitEnable
    {
        get { return _isSubmitEnable; }
        set
        {
            this.RaiseAndSetIfChanged(ref _isSubmitEnable, value);
        }
    }
    //public string StatusText { get; set; }
    private string _statusText;
    public string StatusText
    {
        get { return _statusText; }
        set
        {
            this.RaiseAndSetIfChanged(ref _statusText, value);
        }
    }
    //public string RecordBtnContent { get; set; }
    private string _recordBtnContent;
    public string RecordBtnContent
    {
        get { return _recordBtnContent; }
        set
        {
            this.RaiseAndSetIfChanged(ref _recordBtnContent, value);
        }
    }
    public PronunciationAssessmentResultDto? PronunciationResult { get; set; }

    private object _overAllPlot;
    public object OverAllPlot
    {
        get { return _overAllPlot; }
        set
        {
            this.RaiseAndSetIfChanged(ref _overAllPlot, value);
        }
    }
}
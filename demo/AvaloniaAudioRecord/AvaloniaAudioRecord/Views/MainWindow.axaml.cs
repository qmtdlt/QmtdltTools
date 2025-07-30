using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Structs;

namespace AvaloniaAudioRecord.Views;

public partial class MainWindow : Window
{
    // 將錄音相關物件宣告為類別欄位，以便在不同方法間共享
    private MiniAudioEngine? _audioEngine;
    private AudioCaptureDevice? _captureDevice;
    private Recorder? _recorder;
    private FileStream? _fileStream;
    private readonly string _outputFilePath;

    public MainWindow()
    {
        InitializeComponent();
        
        // 設定錄音檔案的儲存路徑
        _outputFilePath = Path.Combine(Directory.GetCurrentDirectory(), "recording.wav");
    }

    /// <summary>
    /// 點擊開始錄音按鈕時觸發
    /// </summary>
    private void StartRecordAudio(object? sender, RoutedEventArgs e)
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
            // 假設您的按鈕在 XAML 中有名稱，例如 <Button x:Name="StartButton" ...>
            // StartButton.IsEnabled = false;
            // StopButton.IsEnabled = true;
            
            Console.WriteLine("錄音已開始...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"錄音失敗: {ex.Message}");
            // 如果出錯，清理資源
            StopRecordingAndCleanup();
        }
    }

    /// <summary>
    /// 點擊停止錄音按鈕時觸發
    /// </summary>
    private void StopRecordAudio(object? sender, RoutedEventArgs e)
    {
        StopRecordingAndCleanup();
    }

    /// <summary>
    /// 停止錄音並釋放所有相關資源
    /// </summary>
    private void StopRecordingAndCleanup()
    {
        // 檢查是否有正在進行的錄音
        if (_recorder is null || _captureDevice is null)
        {
            return;
        }
        
        Console.WriteLine("正在停止錄音...");

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
        // StartButton.IsEnabled = true;
        // StopButton.IsEnabled = false;
        // PlayButton.IsEnabled = true; // 現在可以播放了
        // SaveButton.IsEnabled = true; // 現在可以儲存了
        
        Console.WriteLine($"錄音已停止，檔案儲存至: {_outputFilePath}");
    }

    private void PlayAudio(object? sender, RoutedEventArgs e)
    {
        // 您可以在此處實作播放 _outputFilePath 的邏輯
        throw new System.NotImplementedException();
    }

    private void SaveAudio(object? sender, RoutedEventArgs e)
    {
        // 錄音已自動儲存，此處可實作 "另存為" 的功能
        throw new System.NotImplementedException();
    }
}
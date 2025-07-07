using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using NAudio.Wave;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Volo.Abp.DependencyInjection;
using System.Text.RegularExpressions;
using QmtdltTools.Domain.Data;
using QmtdltTools.Service.Services;
using Microsoft.AspNetCore.Http;
using QmtdltTools.Domain.Entitys;
using ScottPlot.Palettes;

namespace QmtdltTools.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly TranslationService _translationService;
    public MainWindow(MainWindowVm vm, TranslationService translationService)
    {
        InitializeComponent();
        _translationService = translationService;
        Browser.LifeSpanHandler = new CustomLifeSpanHandler();
        DataContext = vm;
        this.Closing += MainWindow_Closing;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if(DataContext is MainWindowVm vm)
        {
            vm.onClose();
        }
    }

    private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // 获取选中的文本
        if (sender is TextBox textBox)
        {
            string selectedText = textBox.SelectedText;
            // 示例：显示选中的文本
            System.Windows.MessageBox.Show("选中的文本: " + selectedText);
            // 您可以在这里添加其他逻辑，例如：
            Task.Run(async () =>
            {
                VocabularyRecord? findRes = await _translationService.Trans(0, 0, "", selectedText, Guid.Parse("08dd7e88-9af1-4775-8a21-554610976784"));

                Console.WriteLine("asdf");
            });
        }
    }
}

public class MainWindowVm : BindableBase, ISingletonDependency
{
    public MainWindowVm()
    {
        var speechConfig = SpeechConfig.FromEndpoint(new Uri("https://eastus.api.cognitive.microsoft.com/"), ApplicationConst.SPEECH_KEY);
        speechConfig.SpeechRecognitionLanguage = "en-US"; // 设置语音识别语言

        _ = Task.Run(async () =>
        {
            await FromSystemAudio(speechConfig);
        });
    }

    public void onClose()
    {
        isStop = true;
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
    #region ms speech
   

    async Task FromSystemAudio(SpeechConfig speechConfig)
    {
        // 创建 PushAudioInputStream，用于推送音频数据
        using var audioStream = AudioInputStream.CreatePushStream();

        // 使用音频流创建 AudioConfig
        using var audioConfig = AudioConfig.FromStreamInput(audioStream);

        // 创建 SpeechRecognizer
        using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

        // 使用 WasapiLoopbackCapture 捕获系统音频
        using var capture = new WasapiLoopbackCapture();
        capture.WaveFormat = new WaveFormat(16000, 16, 1); // 设置为 16kHz, 16-bit, mono PCM

        // 当捕获到音频数据时，将其写入 audioStream
        capture.DataAvailable += (s, e) =>
        {
            if (e.Buffer.Length > 0)
            {
                audioStream.Write(e.Buffer);
            }
        };

        // 当录音停止时，关闭 audioStream
        capture.RecordingStopped += (s, e) =>
        {
            audioStream.Close();
        };

        // 开始捕获系统音频
        capture.StartRecording();

        // 设置事件处理程序以获取实时识别结果
        speechRecognizer.Recognizing += (s, e) =>
        {
            CurSubtitle = $"{e.Result.Text}";
        };

        speechRecognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                //Console.WriteLine($"已识别: 文本={e.Result.Text}");
                if (!string.IsNullOrEmpty(e.Result.Text))
                {
                    CurSubtitle = e.Result.Text;
                }
            }
        };

        // 开始连续识别
        await speechRecognizer.StartContinuousRecognitionAsync();


        while (!isStop)
        {
            await Task.Delay(500);
        }
        // 停止连续识别
        await speechRecognizer.StopContinuousRecognitionAsync();

        // 停止捕获系统音频
        capture.StopRecording();
    }
    bool isStop = false;
    #endregion
}
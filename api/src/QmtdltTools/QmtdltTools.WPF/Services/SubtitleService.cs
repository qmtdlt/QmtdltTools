using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QmtdltTools.Domain.Data;
using QmtdltTools.WPF.IServices;

namespace QmtdltTools.WPF.Services
{
    
    public class SubtitleService : ISubtitleService, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SubtitleService> _logger;
        private volatile bool _isWorking;
        private bool _isRunning;
        private Action<string>? _setSubtitle;
        private CancellationTokenSource? _cts;
        private SpeechRecognizer? _speechRecognizer;
        private WasapiLoopbackCapture? _capture;

        public SubtitleService(IConfiguration configuration, ILogger<SubtitleService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _isWorking = false;
            _isRunning = false;
        }

        public async Task StartAsync(Action<string> setSubtitle, CancellationToken cancellationToken = default)
        {
            if (_isRunning)
            {
                _logger.LogWarning("SubtitleService is already running.");
                return;
            }

            _setSubtitle = setSubtitle ?? throw new ArgumentNullException(nameof(setSubtitle));
            _isWorking = true;
            _isRunning = true;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                var speechConfig = SpeechConfig.FromEndpoint(new Uri($"https://eastus.api.cognitive.microsoft.com/"), ApplicationConst.SPEECH_KEY);
                speechConfig.SpeechRecognitionLanguage = "en-US";

                // 创建 PushAudioInputStream
                using var audioStream = AudioInputStream.CreatePushStream();
                using var audioConfig = AudioConfig.FromStreamInput(audioStream);

                // 创建 SpeechRecognizer
                _speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

                // 设置事件处理程序
                _speechRecognizer.Recognizing += (s, e) =>
                {
                    if (_isWorking && !string.IsNullOrEmpty(e.Result.Text))
                    {
                        //_setSubtitle.Invoke(e.Result.Text);
                    }
                };

                _speechRecognizer.Recognized += (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech && _isWorking && !string.IsNullOrEmpty(e.Result.Text))
                    {
                        _setSubtitle.Invoke(e.Result.Text);
                        _logger.LogInformation($"Recognized: {e.Result.Text}");
                    }
                };

                _speechRecognizer.SessionStopped += (s, e) =>
                {
                    _logger.LogInformation("Speech recognition session stopped.");
                    StopAsync().GetAwaiter().GetResult();
                };

                // 使用 WasapiLoopbackCapture 捕获系统音频（仅限 Windows）
                _capture = new WasapiLoopbackCapture();
                _capture.WaveFormat = new WaveFormat(16000, 16, 1); // 16kHz, 16-bit, mono PCM

                _capture.DataAvailable += (s, e) =>
                {
                    if (e.Buffer.Length > 0 && _isWorking)
                    {
                        audioStream.Write(e.Buffer);
                    }
                };

                _capture.RecordingStopped += (s, e) =>
                {
                    audioStream.Close();
                    _logger.LogInformation("System audio capture stopped.");
                };

                // 开始捕获和识别
                _capture.StartRecording();
                await _speechRecognizer.StartContinuousRecognitionAsync();

                // 等待取消信号
                await Task.Delay(Timeout.Infinite, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SubtitleService was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubtitleService.");
                throw;
            }
            finally
            {
                await StopAsync();
            }
        }

        public void Pause()
        {
            if (_isWorking)
            {
                _isWorking = false;
                _logger.LogInformation("SubtitleService paused.");
            }
        }

        public void Resume()
        {
            if (!_isWorking)
            {
                _isWorking = true;
                _logger.LogInformation("SubtitleService resumed.");
            }
        }

        public async Task StopAsync()
        {
            if (!_isRunning)
                return;

            _isWorking = false;
            _isRunning = false;

            try
            {
                _cts?.Cancel();
                if (_speechRecognizer != null)
                {
                    await _speechRecognizer.StopContinuousRecognitionAsync();
                    _speechRecognizer.Dispose();
                    _speechRecognizer = null;
                }

                if (_capture != null)
                {
                    _capture.StopRecording();
                    _capture.Dispose();
                    _capture = null;
                }

                _cts?.Dispose();
                _cts = null;
                _setSubtitle = null;

                _logger.LogInformation("SubtitleService stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while stopping SubtitleService.");
            }
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }
    }
}
using System.Text;
using System.Web;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using QmtdltTools.Domain.Data;
using Serilog;
using Microsoft.AspNetCore.Http;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment; // Required for HttpUtility.HtmlEncode

public class MsTTSHelperRest
{
    // Replace with your actual key and region constants
    public static string speechKey = ApplicationConst.SPEECH_KEY;
    public static string speechRegion = ApplicationConst.SPEECH_REGION;

    /// <summary>
    /// Generate by Grok:
    /// Generates speech audio using the Azure Text-to-Speech REST API.
    /// </summary>
    /// <param name="text">The text to synthesize.</param>
    /// <param name="SpeechSynthesisVoiceName">The name of the voice to use (e.g., "en-US-AvaMultilingualNeural").</param>
    /// <returns>A byte array containing the audio data (MP3 format).</returns>
    /// <exception cref="Exception">Thrown if the REST API call fails.</exception>
    public static byte[] GetSpeakStreamRest(string text, string SpeechSynthesisVoiceName)
    {
        using (var client = new HttpClient())
        {
            var escapedText = HttpUtility.HtmlEncode(text);
            string languageCode = "en-US"; // Default or extract from voice name if possible
            var voiceNameParts = SpeechSynthesisVoiceName.Split('-');
            if (voiceNameParts.Length >= 2)
            {
                languageCode = $"{voiceNameParts[0]}-{voiceNameParts[1]}";
            }

            var ssml = $@"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='http://www.microsoft.com/css/local' xml:lang='{languageCode}'>
    <voice name='{SpeechSynthesisVoiceName}'>
        {escapedText}
    </voice>
</speak>";

            // Set request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", speechKey);
            // Specify the audio format - matching the SDK's Audio16Khz32KBitRateMonoMp3
            client.DefaultRequestHeaders.Add("X-Microsoft-OutputFormat", "audio-16khz-32kbitrate-mono-mp3");
            // Set a User-Agent header (recommended)
            client.DefaultRequestHeaders.Add("User-Agent", "YourAppName"); // Replace with your application name

            // Construct the endpoint URL
            var endpoint = $"https://{speechRegion}.tts.speech.microsoft.com/cognitiveservices/v1";

            // Create the request content with SSML body and content type
            var content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml");

            try
            {
                HttpResponseMessage response = client.PostAsync(endpoint, content).GetAwaiter().GetResult();

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Read the audio data from the response body as a byte array
                    byte[] audioBytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                    return audioBytes;
                }
                else
                {
                    // Read the error message from the response
                    string errorMessage = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    // Log the error (replace with your logging mechanism)
                    // Log.Error($"Speech synthesis REST API failed: {response.StatusCode} - {errorMessage}");
                    Log.Error($"Speech synthesis REST API failed: {response.StatusCode} - {errorMessage}"); // Example logging
                    // Return empty array or throw exception based on desired behavior
                    return new byte[0];
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred during REST API call: {ex.Message},content:{text}"); // Example logging
                return new byte[0];
            }
        }
    }

    /// <summary>
    /// Generate by Grok:
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static byte[] GetSpeakStream(string text, string SpeechSynthesisVoiceName = ApplicationConst.DefaultVoiceName)
    {
        // Create speech configuration
        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);

        speechConfig.SpeechSynthesisVoiceName = SpeechSynthesisVoiceName;

        // Set the output format to MP3
        speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz32KBitRateMonoMp3);

        using (var memoryStream = new MemoryStream())
        {
            // Create our callback handler
            var callback = new CustomPushAudioOutputStreamCallback(memoryStream);

            // Create the push audio output stream with our callback
            using (var pushStream = new PushAudioOutputStream(callback))
            {
                using (var audioConfig = AudioConfig.FromStreamOutput(pushStream))
                {
                    using (var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig))
                    {
                        // Synthesize the text to speech
                        var result = synthesizer.SpeakTextAsync(text).GetAwaiter().GetResult();

                        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                        {
                            return memoryStream.ToArray();
                        }
                        else
                        {
                            Log.Error($"Speech synthesis failed: {result.Reason}");
                            return new byte[0];
                        }
                    }
                }
            }
        }
    }


    public static async Task<PronunciationAssessmentResult?> AssessPronunciationAsync(IFormFile audioFile, string referenceText)
    {
        if (audioFile == null || string.IsNullOrEmpty(referenceText))
        {
            throw new ArgumentException("Audio file and reference text are required.");
        }

        // 创建 SpeechConfig
        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        speechConfig.SpeechRecognitionLanguage = "en-US"; // 设置语言

        // 创建 PronunciationAssessmentConfig
        var pronunciationConfig = new PronunciationAssessmentConfig(referenceText,
            GradingSystem.HundredMark, Granularity.Phoneme);

        // 将 IFormFile 转换为流
        using var audioStream = audioFile.OpenReadStream();

        // 创建 AudioConfig
        using var reader = new BinaryReader(audioStream);

        // 创建自定义回调实例
        var callback = new BinaryReaderCallback(reader);

        // 创建 PullAudioInputStream
        using var audioInputStream = new PullAudioInputStream(callback);

        // 创建 AudioConfig
        using var audioConfig = AudioConfig.FromStreamInput(audioInputStream);

        // 创建 SpeechRecognizer
        using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

        // 应用发音评估配置
        pronunciationConfig.ApplyTo(recognizer);

        // 进行识别
        var result = await recognizer.RecognizeOnceAsync();

        // 检查识别结果
        if (result.Reason == ResultReason.RecognizedSpeech)
        {
            // 获取发音评估结果
            var pronunciationResult = PronunciationAssessmentResult.FromResult(result);
            //var score = pronunciationResult.PronunciationScore;
            //var accuracy = pronunciationResult.AccuracyScore;
            //var fluency = pronunciationResult.FluencyScore;
            //var completeness = pronunciationResult.CompletenessScore;
            return pronunciationResult;

            // 返回评估结果（JSON 格式）
            //return $@"
            //{{
            //    ""PronunciationScore"": {score},
            //    ""AccuracyScore"": {accuracy},
            //    ""FluencyScore"": {fluency},
            //    ""CompletenessScore"": {completeness}
            //}}";
        }
        else if (result.Reason == ResultReason.NoMatch)
        {
            //return "No speech could be recognized.";
            return null;
        }
        else if (result.Reason == ResultReason.Canceled)
        {
            var cancellation = CancellationDetails.FromResult(result);
            //return $"Recognition canceled: {cancellation.Reason}. Error details: {cancellation.ErrorDetails}";
            return null;
        }
        else
        {
            //return "Unknown error occurred.";
            return null;
        }
    }


    private class BinaryReaderStream : PullAudioInputStreamCallback
    {
        private readonly BinaryReader _reader;

        public BinaryReaderStream(Stream stream)
        {
            _reader = new BinaryReader(stream);
        }

        public override int Read(byte[] dataBuffer, uint size)
        {
            return _reader.Read(dataBuffer, 0, (int)size);
        }

        public override void Close()
        {
            _reader.Dispose();
        }
    }
    /// <summary>
    /// Generate by Grok: 
    /// strem callback
    /// </summary>
    private class CustomPushAudioOutputStreamCallback : PushAudioOutputStreamCallback
    {
        private readonly MemoryStream _stream;

        public CustomPushAudioOutputStreamCallback(MemoryStream stream)
        {
            _stream = stream;
        }

        public override uint Write(byte[] dataBuffer)
        {
            _stream.Write(dataBuffer, 0, dataBuffer.Length);
            return (uint)dataBuffer.Length;
        }

        public override void Close()
        {
            // No additional cleanup needed as MemoryStream is managed by the using statement
        }
    }

    public class BinaryReaderCallback : PullAudioInputStreamCallback
    {
        private readonly BinaryReader _reader;

        public BinaryReaderCallback(BinaryReader reader)
        {
            _reader = reader;
        }

        // 实现 Read 方法，从 BinaryReader 读取数据
        public override int Read(byte[] dataBuffer, uint size)
        {
            try
            {
                // 从流中读取最多 size 个字节的数据
                int bytesRead = _reader.Read(dataBuffer, 0, (int)size);
                return bytesRead;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取流时出错: {ex.Message}");
                return 0;
            }
        }

        // 实现 Close 方法，清理资源
        public override void Close()
        {
            _reader?.Close();
            base.Close();
        }
    }
}
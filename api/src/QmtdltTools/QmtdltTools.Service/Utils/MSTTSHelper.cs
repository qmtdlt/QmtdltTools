using System.Text;
using System.Web;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using QmtdltTools.Domain.Data;
using Serilog;
using Microsoft.AspNetCore.Http;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using System.Threading.Tasks; // Required for HttpUtility.HtmlEncode

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

    public static async Task<PronunciationAssessmentResult?> PronunciationAssessmentWithWavFileAsync(IFormFile audioFile, string referenceText)
    {
        // 1. 保存上传的音频为本地临时文件
        string tempFile = Path.GetTempFileName();
        string wavFile = Path.ChangeExtension(tempFile, ".wav");
        using (var fs = new FileStream(wavFile, FileMode.Create, FileAccess.Write))
        {
            await audioFile.CopyToAsync(fs);
        }

        try
        {
            // 2. 创建 SpeechConfig
            var speechKey = ApplicationConst.SPEECH_KEY;
            var speechRegion = ApplicationConst.SPEECH_REGION;
            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            speechConfig.SpeechRecognitionLanguage = "en-US";

            // 3. 用FromWavFileInput创建AudioConfig
            using var audioConfig = AudioConfig.FromWavFileInput(wavFile);

            // 4. 创建 SpeechRecognizer
            using var recognizer = new SpeechRecognizer(speechConfig, "en-US", audioConfig);

            // 5. 设置发音评估参数
            var pronConfig = new PronunciationAssessmentConfig(
                referenceText,
                GradingSystem.HundredMark,
                Granularity.Phoneme,
                enableMiscue: false
            );
            pronConfig.EnableProsodyAssessment();
            pronConfig.ApplyTo(recognizer);

            // 6. 开始识别
            var result = await recognizer.RecognizeOnceAsync();

            // 7. 判断并返回结果
            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                return PronunciationAssessmentResult.FromResult(result);
            }
            else
            {
                // 可选：记录NoMatch/Canceled等情况
                return null;
            }
        }
        finally
        {
            // 8. 删除临时文件
            if (File.Exists(wavFile)) File.Delete(wavFile);
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    public static async Task<PronunciationAssessmentResult> PronunciationAssessmentWithStream(IFormFile audioFile, string referenceText)
    {
        // Creates an instance of a speech config with specified endpoint and subscription key.
        // Replace with your own endpoint and subscription key.
        var config = SpeechConfig.FromEndpoint(new Uri($"https://{speechRegion}.api.cognitive.microsoft.com"), speechKey);

        using var audioStream = audioFile.OpenReadStream();

        // audioFile 存文件
        //using (var fs = new FileStream(audioFile.FileName, FileMode.Create))
        //{
        //    audioStream.CopyTo(fs);
        //}

        // Read audio data from file. In real scenario this can be from memory or network
        var audioDataWithHeader = audioStream.GetAllBytes(); //File.ReadAllBytes(audioFile.FileName);

        var audioData = new byte[audioDataWithHeader.Length - 46];
        Array.Copy(audioDataWithHeader, 46, audioData, 0, audioData.Length);

        var resultReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var resultContainer = new List<string>();

        var startTime = DateTime.Now;

        var res = await PronunciationAssessmentWithStreamInternalAsync(config, referenceText, audioData, resultReceived, resultContainer);
        //if (File.Exists(audioFile.FileName))
        //{
        //    File.Delete(audioFile.FileName);
        //}
        return res;
    }
    private static async Task<PronunciationAssessmentResult> PronunciationAssessmentWithStreamInternalAsync(SpeechConfig speechConfig, string referenceText, byte[] audioData, TaskCompletionSource<int> resultReceived, List<string> resultContainer)
    {
        using (var audioInputStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1))) // This need be set based on the format of the given audio data
        using (var audioConfig = AudioConfig.FromStreamInput(audioInputStream))
        // Specify the language used for Pronunciation Assessment.
        using (var speechRecognizer = new SpeechRecognizer(speechConfig, "en-US", audioConfig))
        {
            // create pronunciation assessment config, set grading system, granularity and if enable miscue based on your requirement.
            var pronAssessmentConfig = new PronunciationAssessmentConfig(referenceText, GradingSystem.HundredMark, Granularity.Phoneme, false);

            pronAssessmentConfig.EnableProsodyAssessment();

            speechRecognizer.SessionStarted += (s, e) => {
                Console.WriteLine($"SESSION ID: {e.SessionId}");
            };

            pronAssessmentConfig.ApplyTo(speechRecognizer);

            audioInputStream.Write(audioData);
            audioInputStream.Write(new byte[0]); // send a zero-size chunk to signal the end of stream

            var result = await speechRecognizer.RecognizeOnceAsync();
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
}
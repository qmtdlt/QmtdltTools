using System.Text.RegularExpressions;
using VersOne.Epub;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
namespace QmtdltTools.Service.Utils
{
    
    public class EpubHelper
    {
        public static string speechKey = Environment.GetEnvironmentVariable("SPEECH_KEY");
        public static string speechRegion = Environment.GetEnvironmentVariable("SPEECH_REGION");

        

        
        public static string ConvertEpubToText(string epubFilePath)
        {
            if (!File.Exists(epubFilePath))
            {
                Console.WriteLine("文件不存在: " + epubFilePath);
                return "";
            }

            try
            {
                // 根据 epub 文件生成 txt 文件路径（扩展名替换为 .txt）
                string txtFilePath = Path.ChangeExtension(epubFilePath, ".txt");

                Console.WriteLine(txtFilePath);
                // 使用 VersOne.Epub 库读取 EPUB 文件
                EpubBook epubBook = EpubReader.ReadBook(epubFilePath);

                // 用于保存提取的纯文本内容
                var combinedText = "";

                // EPUB 文件中的正文内容通常保存在 ReadingOrder 中
                foreach (var item in epubBook.ReadingOrder)
                {
                    // 每个 item 的 Content 属性包含 HTML 格式的内容
                    if (!string.IsNullOrEmpty(item.Content))
                    {
                        // 使用正则表达式剥离 HTML 标签，得到纯文本
                        string plainText = Regex.Replace(item.Content, "<[^>]+>", " ");
                        // 可进一步对文本进行整理，如去除多余空格
                        plainText = Regex.Replace(plainText, "\\s+", " ").Trim();

                        combinedText += plainText + Environment.NewLine + Environment.NewLine;
                    }
                }

                // 将结果写入 .txt 文件
                File.WriteAllText(txtFilePath, combinedText);
                Console.WriteLine("转换完成！输出文件：" + txtFilePath);
                return txtFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine("处理过程中发生错误: " + ex.Message);
            }

            return "";
        }


        public static EpubBook GetEbook(string epubFilePath, out string message)
        {
            message = "";
            if (!File.Exists(epubFilePath))
            {
                message = "file not exists: " + epubFilePath;
                return null;
            }

            try
            {
                // 根据 epub 文件生成 txt 文件路径（扩展名替换为 .txt）
                string txtFilePath = Path.ChangeExtension(epubFilePath, ".txt");

                Console.WriteLine(txtFilePath);
                // 使用 VersOne.Epub 库读取 EPUB 文件
                return EpubReader.ReadBook(epubFilePath);

                
            }
            catch (Exception ex)
            {
                message = "some exception occur: " + ex.Message;
            }

            return null;
        }

        public static byte[] GetSpeakStream(string text)
        {
            // Create speech configuration
            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            speechConfig.SpeechSynthesisVoiceName = "zh-CN-YunxiNeural";

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
                                throw new Exception($"Speech synthesis failed: {result.Reason}");
                            }
                        }
                    }
                }
            }
        }

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
}

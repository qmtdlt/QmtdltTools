using System.Text.RegularExpressions;
using VersOne.Epub;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using HtmlAgilityPack;
using System.Text;
using QmtdltTools.Domain.Models;
using Serilog;
namespace QmtdltTools.Service.Utils
{
    
    public class EpubHelper
    {
        public static string speechKey = Environment.GetEnvironmentVariable("SPEECH_KEY");
        public static string speechRegion = Environment.GetEnvironmentVariable("SPEECH_REGION");


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

        /// <summary>
        /// Generate by Grok:
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static byte[] GetSpeakStream(string text,string SpeechSynthesisVoiceName)
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

        public static List<MyPragraph> PrepareAllPragraphs(EpubBook book)
        {
            List<MyPragraph> mybook = new List<MyPragraph>();

            foreach (EpubLocalTextContentFile textContentFile in book.ReadingOrder)
            {
                var chapter = getChapterPragraphs(textContentFile);
                if (null != chapter)
                {
                    mybook.AddRange(chapter);
                }
            }
            return mybook;
        }

        static List<MyPragraph> getChapterPragraphs(EpubLocalTextContentFile textContentFile)
        {
            HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(textContentFile.Content);
            StringBuilder sb = new();
            foreach (HtmlNode node in htmlDocument.DocumentNode.SelectNodes("//text()"))
            {
                string paragraph = node.InnerText.Trim();
                if (!string.IsNullOrEmpty(paragraph))
                {
                    sb.AppendLine(paragraph);
                }
            }
            string chapterText = sb.ToString();
            if (!string.IsNullOrEmpty(chapterText))
            {
                return GetParagraph(chapterText);
            }
            return null;
        }

        // 章节分段落
        static List<MyPragraph> GetParagraph(string chapterText)
        {
            List<MyPragraph> res = new List<MyPragraph>();
            string[] splitPragraphs = chapterText.Split("\r\n");
            foreach (string pragraphs in splitPragraphs)
            {
                if (!string.IsNullOrEmpty(pragraphs.Trim()))
                {
                    MyPragraph myPragraph = new MyPragraph
                    {
                        PragraphText = pragraphs,
                        Sentences = GetSentences(pragraphs)
                    };
                    if (myPragraph.Sentences.Count <= 0)
                        continue;
                    res.Add(myPragraph);
                }
            }
            return res;
        }

        static char[] splitSymbols = new List<char> { '；', ';', ':', '：', '。', '.' }.ToArray();
        // 段落分句子
        static List<string> GetSentences(string pragraphs)
        {
            return pragraphs.Split(splitSymbols).Where(t => !string.IsNullOrEmpty(t)).ToList();
        }
    }
}

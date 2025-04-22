using System.Text.RegularExpressions;
using VersOne.Epub;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using HtmlAgilityPack;
using System.Text;
using QmtdltTools.Domain.Data;
using QmtdltTools.Domain.Models;
using Serilog;
using System;
namespace QmtdltTools.Service.Utils
{
    
    public class EpubHelper
    {
        public static string speechKey = ApplicationConst.SPEECH_KEY;
        public static string speechRegion = ApplicationConst.SPEECH_REGION;

        public static EpubBook? GetEbook(string epubFilePath, out string message)
        {
            message = "";
            if (!File.Exists(epubFilePath))
            {
                message = "file not exists: " + epubFilePath;
                return null;
            }
            try
            {
                // 使用 VersOne.Epub 库读取 EPUB 文件
                return EpubReader.ReadBook(epubFilePath);
            }
            catch (Exception ex)
            {
                message = "some exception occur: " + ex.Message;
            }
            return null;
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
            string[] splitPragraphs = chapterText.Replace("\r","").Split("\n");          // wiwndow \r\n
            string pragraph = "";
            for (int i = 0; i < splitPragraphs.Length; i++)
            {
                pragraph = splitPragraphs[i].Trim();        
                if (i+1<splitPragraphs.Length && isTooShort(splitPragraphs[i]))
                {
                    pragraph += splitPragraphs[i + 1].Trim();        // 合并段落
                    i++;
                    if(i+2 <  splitPragraphs.Length)
                    {
                        pragraph += splitPragraphs[i + 2].Trim();        // 合并段落
                        i++;
                    }
                }
                if (!string.IsNullOrEmpty(pragraph))
                {
                    MyPragraph myPragraph = new MyPragraph
                    {
                        PragraphText = pragraph,
                        Sentences = GetSentences(pragraph)
                    };
                    if (myPragraph.Sentences.Count <= 0)
                        continue;
                    if(res.Count == 0)
                    {
                        myPragraph.isFirst = true;        // 第一段
                    }
                    res.Add(myPragraph);
                }
            }
            return res;
        }
        static bool isTooShort(string input)
        {
            if(input.Length<=1)
            {
                // 单个字符
                return true;
            }
            var words = input.Split(',','.');
            if(words.Length <= 3)
            {
                return true;        // 单词数小于登录3
            }
            return false;
        }

        static char[] splitSymbols = new List<char> { '；', ';', ':', '：', '。', '.' }.ToArray();
        // 段落分句子
        static List<string> GetSentences(string pragraphs)
        {
            return pragraphs.Split(splitSymbols).Where(t => !string.IsNullOrEmpty(t)).ToList();
        }
    }
}

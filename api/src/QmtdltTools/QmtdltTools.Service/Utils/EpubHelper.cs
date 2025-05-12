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
                List<MyPragraph> chapter = getChapterPragraphs(textContentFile);
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
            HtmlNodeCollection blockNodes = htmlDocument.DocumentNode.SelectNodes("//p | //div");
            if (blockNodes != null)
            {
                foreach (HtmlNode blockNode in blockNodes)
                {
                    // 获取块级元素内的所有文本节点
                    var textNodes = blockNode.SelectNodes(".//text()");
                    if (textNodes != null)
                    {
                        // 合并文本节点，移除多余空白
                        string blockText = string.Join(" ", textNodes
                            .Select(node => node.InnerText.Trim())
                            .Where(text => !string.IsNullOrWhiteSpace(text)));

                        // 添加到结果，并保留段落间换行
                        if (!string.IsNullOrEmpty(blockText))
                        {
                            sb.AppendLine(blockText);
                        }
                    }
                }
            }
            else
            {
                // 如果没有明确的块级元素，退回到整个文档的文本处理
                var textNodes = htmlDocument.DocumentNode.SelectNodes("//text()");
                if (textNodes != null)
                {
                    string allText = string.Join(" ", textNodes
                        .Select(node => node.InnerText.Trim())
                        .Where(text => !string.IsNullOrWhiteSpace(text)));
                    sb.AppendLine(allText);
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
            if(HasNoPunctuation(chapterText))
            {
                chapterText = chapterText.Replace("\r", " ").Replace("\n", " ").Replace("  "," ");        // 直接去掉换行
            }
            string[] splitPragraphs = chapterText.Replace("\r","").Split("\n");          // wiwndow \r\n
            string pragraph = "";
            for (int i = 0; i < splitPragraphs.Length; i++)
            {
                pragraph = splitPragraphs[i].Trim();        
                if (i+1<splitPragraphs.Length)
                {
                    var isTooShortResult = isTooShort(splitPragraphs[i]);
                    if (1 == isTooShortResult)
                    {
                        // 单个字符
                        pragraph += splitPragraphs[i + 1].Trim();        // 合并段落
                        i++;
                    }
                    else if (2 == isTooShortResult)
                    {
                        // 特别短的段落
                        pragraph += (" " + splitPragraphs[i + 1].Trim());        // 合并段落,中间需要空格
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
        static bool HasNoPunctuation(string input)
        {
            // 定义正则表达式，匹配指定的英文和中文标点符号
            string pattern = @"[,.\?:!;，。？：！；]";
            // 使用 Regex.IsMatch 检查是否包含这些标点
            return !Regex.IsMatch(input, pattern);
        }
        static int isTooShort(string input)
        {
            if(input.Length<=1)
            {
                // 单个字符
                return 1;
            }
            var words = input.Split(',','.');
            if(words.Length <= 3)
            {
                return 2;        // 单词数小于登录3
            }
            return -1;
        }

        static char[] splitSymbols = new List<char> { '；', ';', ':', '：', '。', '.', ',', '，' }.ToArray();
        // 段落分句子
        static List<string> GetSentences(string pragraphs)
        {
            var sts = pragraphs.Split(splitSymbols).Select(t=>t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
            return sts;
        }
    }
}

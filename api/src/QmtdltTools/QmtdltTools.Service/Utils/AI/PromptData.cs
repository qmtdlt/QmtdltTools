using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QmtdltTools.Service.Utils.AI
{
    public class PromptData
    {
        public static string SentenceExplainerPrompt(string phase)
        {
            return $@"你是我的英语辅导老师，我在通过阅读学习英语，我已经翻译了所有生词，但是还是不能理解整个句子，我将给你一个段落，请你给我逐句讲解一下（我给你的段落可能摘录自一本书，必要的话结合背景去理解段落），段落如下
{phase}
请按照如下格式回复
例句1：
解释：
例句2：
解释：";
        }

        public static string EnglishArticleGenerateFromChinese(string chineseArticle)
        {
            return $@"你是我的英语辅导老师，我想要将一段中文文章翻译成英文,并进行润色，符合英语沐浴者的阅读写习惯，要求如下：" +
                   $"\n1. 文章内容是：{chineseArticle}" +
                   "\n2. 文章内容要符合英语语法规范，且通顺易懂。" +
                   "\n3. 文章内容要尽量保留原文的意思和风格。" +
                   "\n4. 直接返回英文段落字符串即可。";    
        }
    }
}

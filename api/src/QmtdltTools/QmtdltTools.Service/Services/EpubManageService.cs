using HtmlAgilityPack;
using QmtdltTools.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersOne.Epub;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Service.Services
{
    public class EpubManageService : ITransientDependency
    {
        public EpubManageService()
        {

        }

        public async Task<Response<bool>> UploadEpub(EpubBook book)
        {
            int cnt = 0;
            foreach (EpubLocalTextContentFile textContentFile in book.ReadingOrder)
            {
                PrintTextContentFile(textContentFile);
                Console.WriteLine(cnt++);
            }
            return new Response<bool>
            {
                data = true
            };
        }

        private static void PrintTextContentFile(EpubLocalTextContentFile textContentFile)
        {
            HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(textContentFile.Content);
            StringBuilder sb = new();
            foreach (HtmlNode node in htmlDocument.DocumentNode.SelectNodes("//text()"))
            {
                sb.AppendLine(node.InnerText.Trim());
            }
            string contentText = sb.ToString();
            Console.WriteLine(contentText);
            Console.WriteLine();
        }
    }
}

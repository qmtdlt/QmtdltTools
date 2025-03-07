using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.Domain.Models;
using QmtdltTools.EFCore;
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
        private readonly DC _dc;
        public EpubManageService(DC dc)
        {
            _dc = dc;
        }

        public async Task<Response<bool>> UploadEpub(byte[] buffer)
        {
            using (var ms = new MemoryStream(buffer))
            {
                EpubBook book = EpubReader.ReadBook(ms);
                // 如果存在相同 book.Title 和 book.Author 的电子书，则不再插入
                if (_dc.EBooks.Any(e => e.Title == book.Title && e.Author == book.Author))
                {
                    return new Response<bool>
                    {
                        code = 1,
                        message = "电子书已存在"
                    };
                }
                else
                {
                    EBookMain eBookMain = new EBookMain
                    {
                        Title = book.Title,
                        Author = book.Author,
                        CoverImage = book.CoverImage,
                        BookBin = buffer
                    };
                    _dc.EBooks.Add(eBookMain);
                    await _dc.SaveChangesAsync();
                }
            }
            return new Response<bool>
            {
                data = true
            };
            //_dc.

            //int cnt = 0;
            //foreach (EpubLocalTextContentFile textContentFile in book.ReadingOrder)
            //{
            //    PrintTextContentFile(textContentFile);
            //    Console.WriteLine(cnt++);
            //}
            //return new Response<bool>
            //{
            //    data = true
            //};
        }

        public async Task<EBookMain?> GetBookById(Guid id)
        {
            var book = await _dc.EBooks.Where(t=>t.Id == id).FirstOrDefaultAsync();
            return book;
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

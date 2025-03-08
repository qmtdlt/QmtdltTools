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

        public async Task<Response<bool>> UploadEpub(byte[] buffer,string fileName)
        {
            using (var ms = new MemoryStream(buffer))
            {
                // 将buffer存储搭配wwwroot下
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot","uploads","epubs", fileName);
                // 判断路径是否存在，不存在则创建
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                }
                
                using (var fs = new FileStream(path, FileMode.Create))
                {
                    await ms.CopyToAsync(fs);
                }
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
                        CoverImage = Convert.ToBase64String(book.CoverImage),
                        BookPath = path
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

        public async Task<List<EBookMain>> GetBooks()
        {
            return await _dc.EBooks.ToListAsync();
        }

        public async Task<Response<bool>> DeleteBook(Guid id)
        {
            var book = await _dc.EBooks.Where(t => t.Id == id).FirstOrDefaultAsync();
            if (book == null)
            {
                return new Response<bool>
                {
                    code = 1,
                    message = "电子书不存在"
                };
            }
            else
            {
                _dc.EBooks.Remove(book);
                await _dc.SaveChangesAsync();
                return new Response<bool>
                {
                    data = true
                };
            }
        }
    }
}

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
using Microsoft.AspNetCore.Http;
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

        public async Task<Response<bool>> UploadEpub(byte[] buffer,string fileName,Guid? uid)
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
                if (_dc.EBooks.Any(e => e.Title == book.Title && e.Author == book.Author && e.CreateBy == uid))
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
                        BookPath = path,
                        CreateBy = uid,
                    };
                    _dc.EBooks.Add(eBookMain);
                    await _dc.SaveChangesAsync();
                }
            }
            return new Response<bool>
            {
                data = true
            };
        }

        public async Task<EBookMain?> GetBookById(Guid id)
        {
            var book = await _dc.EBooks.Where(t=>t.Id == id).FirstOrDefaultAsync();
            return book;
        }
        public async Task<List<EBookMain>> GetBooks(Guid? uid)
        {
            return await _dc.EBooks.Where(t=>t.CreateBy == uid).ToListAsync();
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
                try
                {
                    File.Delete(book.BookPath);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                return new Response<bool>
                {
                    data = true
                };
            }
        }
    }
}

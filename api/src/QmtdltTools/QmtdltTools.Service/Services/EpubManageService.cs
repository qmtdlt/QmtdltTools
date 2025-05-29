using Microsoft.EntityFrameworkCore;
using QmtdltTools.Domain.Data;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.Domain.Models;
using QmtdltTools.EFCore;
using System.IO;
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

        public async Task<Response<bool>> ExcerptChapter(string content, Guid? uid)
        {
            // 将buffer存储搭配wwwroot下
            string fileName = $"摘录{DateTime.Now.ToString(ApplicationConst.TimeFormat)}";
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "epubs", fileName);
            // 判断路径是否存在，不存在则创建
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            File.WriteAllText(path, content);


            if (_dc.EBooks.Any(e => e.Title == fileName && e.Author == uid.ToString() && e.CreateBy == uid))
            {
                return new Response<bool>
                {
                    code = 1,
                    message = "文件已存在"
                };
            }
            else
            {
                EBookMain eBookMain = new EBookMain
                {
                    Title = fileName,
                    Author = uid.ToString(),
                    CoverImage = null,
                    BookPath = path,
                    CreateBy = uid,
                    BookType = BookTypes.Txt
                };
                _dc.EBooks.Add(eBookMain);
                await _dc.SaveChangesAsync();
            }
            return new Response<bool>
            {
                data = true
            };
        }
        public async Task<Response<bool>> UploadText(Stream stream, string fileName, Guid? uid)
        {
            using (stream)
            {
                // 将buffer存储搭配wwwroot下
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "epubs", fileName);
                // 判断路径是否存在，不存在则创建
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                }

                using (var fs = new FileStream(path, FileMode.Create))
                {
                    await stream.CopyToAsync(fs);
                }


                if (_dc.EBooks.Any(e => e.Title == fileName && e.Author == uid.ToString() && e.CreateBy == uid))
                {
                    return new Response<bool>
                    {
                        code = 1,
                        message = "文件已存在"
                    };
                }
                else
                {
                    EBookMain eBookMain = new EBookMain
                    {
                        Title = fileName,
                        Author = uid.ToString(),
                        CoverImage = null,
                        BookPath = path,
                        CreateBy = uid,
                        BookType = BookTypes.Txt
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
                        CoverImage = book.CoverImage,
                        BookPath = path,
                        CreateBy = uid,
                        BookType = BookTypes.Txt
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
        public async Task<List<EBookMain>> GetBooks(Guid? uid, string bookType)
        {
            return await _dc.EBooks.Where(t=>t.CreateBy == uid && t.BookType == bookType).ToListAsync();
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

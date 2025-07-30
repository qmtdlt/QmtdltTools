using Volo.Abp.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using QmtdltTools.Service.Services;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.Domain.Models;
using VersOne.Epub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using QmtdltTools.Extensions;
using QmtdltTools.Domain.Data;

namespace QmtdltTools.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class EpubManageController:AbpController
    {
        private readonly EpubManageService _epubManageService;
        private readonly GuestQuotaLimitService _guestQuotaLimitService;
        public EpubManageController(EpubManageService epubManageService, GuestQuotaLimitService guestQuotaLimitService)
        {
            _epubManageService = epubManageService;
            _guestQuotaLimitService = guestQuotaLimitService;
        }
        // epub 文件上传接口
        [HttpPost("UploadEpub")]
        public async Task<Response<bool>> UploadEpub(IFormFile file)
        {
            Guid? uid = HttpContext.GetUserId();           // 当前登录用户id
            string? uidStr = HttpContext.GetUserIdStr();           // 当前登录用户id

            if (await _guestQuotaLimitService.IsLimited(uidStr, "UploadBook", ApplicationConst.QuotaBookCnt))
            {
                return new Response<bool>
                {
                    code = 1,
                    message = "上传电子书次数已达上限，请登录或注册用户，或将在下个月重置配额"
                };
            }

            await _guestQuotaLimitService.AddUsage(uidStr, "UploadBook");

            var bytes = file.GetAllBytes();         // 文件转换为字节数组
            
            return await _epubManageService.UploadEpub(bytes,file.FileName,uid);
        }
        [HttpPost("UploadTxt")]
        public async Task<Response<bool>> UploadTxt(IFormFile file)
        {

            Guid? uid = HttpContext.GetUserId();           // 当前登录用户id
            var stream = file.OpenReadStream();         // 文件转换为字节数组

            return await _epubManageService.UploadText(stream, file.FileName, uid);
        }
        [HttpPost("ExcerptChapter")]
        public async Task<Response<bool>> ExcerptChapter(string content)
        {

            Guid? uid = HttpContext.GetUserId();           // 当前登录用户id

            return await _epubManageService.ExcerptChapter(content, uid);
        }
        [HttpGet("DownloadEpub")]
        public async Task<IActionResult> DownloadEpub(Guid id)
        {
            try
            {
                var book = await _epubManageService.GetBookById(id); 

                if(book == null) return NotFound("电子书不存在");

                // 返回文件内容，指定MIME类型
                var bytes = System.IO.File.ReadAllBytes(book.BookPath);
                return File(new MemoryStream(bytes), "application/epub+zip");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"服务器错误: {ex.Message}");
            }
        }
        // 获取book列表
        [HttpGet("GetBooks")]
        public async Task<List<EBookMain>> GetBooks(string booktype)
        {
            return await _epubManageService.GetBooks(HttpContext.GetUserId(), booktype);
        }
        // 删除book，DeleteBook
        [HttpDelete("DeleteBook")]
        public async Task<Response<bool>> DeleteBook(Guid id)
        {
            return await _epubManageService.DeleteBook(id);
        }
    }
}

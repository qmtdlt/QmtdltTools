using Volo.Abp.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using QmtdltTools.Service.Services;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.Domain.Models;
using VersOne.Epub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using QmtdltTools.Extensions;

namespace QmtdltTools.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class EpubManageController:AbpController
    {
        private readonly EpubManageService _epubManageService;
        public EpubManageController(EpubManageService epubManageService)
        {
            _epubManageService = epubManageService;
        }
        // epub 文件上传接口
        [HttpPost("UploadEpub")]
        public async Task<Response<bool>> UploadEpub(IFormFile file)
        {
            
            Guid? uid = HttpContext.GetUserId();           // 当前登录用户id
            var bytes = file.GetAllBytes();         // 文件转换为字节数组
            
            return await _epubManageService.UploadEpub(bytes,file.FileName,uid);
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
        public async Task<List<EBookMain>> GetBooks()
        {
            var userId = HttpContext.GetUserId();           // 当前登录用户id

            return await _epubManageService.GetBooks(HttpContext.GetUserId());
        }
        // 删除book，DeleteBook
        [HttpDelete("DeleteBook")]
        public async Task<Response<bool>> DeleteBook(Guid id)
        {
            return await _epubManageService.DeleteBook(id);
        }
    }
    
    
}

using Volo.Abp.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using QmtdltTools.Service.Services;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.Domain.Models;
using VersOne.Epub;

namespace QmtdltTools.Controllers
{
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
            EpubBook book = EpubReader.ReadBook(file.OpenReadStream());
            return await _epubManageService.UploadEpub(book);
        }
        //// 下载epub，返回url
        //[HttpGet("DownloadEpub")]
        //public async Task<byte[]> DownloadEpub()
        //{
        //    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "epubs", "test.epub");
        //    if(System.IO.File.Exists(filePath))
        //    {
        //        //var url = $"{Request.Scheme}://{Request.Host}/epubs/test.epub";
        //        return System.IO.File.ReadAllBytes(filePath);
        //    }
        //    throw new Exception(filePath + "不存在！");
        //}
        [HttpGet("DownloadEpub")]
        public async Task<IActionResult> DownloadEpub()
        {
            try
            {
                // 获取epub文件路径
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "epubs", "test.epub");

                // 检查文件是否存在
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("电子书文件不存在");
                }

                // 读取文件为字节数组
                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

                // 返回文件内容，指定MIME类型
                return File(fileBytes, "application/epub+zip");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"服务器错误: {ex.Message}");
            }
        }

    }
}

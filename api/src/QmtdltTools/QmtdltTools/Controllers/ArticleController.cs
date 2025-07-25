using Volo.Abp.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using QmtdltTools.Service.Services;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Polly;
using QmtdltTools.Domain.Dtos;
using QmtdltTools.Extensions;
using System.Threading.Tasks;

namespace QmtdltTools.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ArticleController : AbpController
    {
        private readonly AiApiService _service;
        public ArticleController(AiApiService service)
        {
            _service = service;
        }
        
        [HttpPost("GetEnglishArticle")]
        public async Task<string?> GetEnglishArticle(string chineseArticle)
        {
            return await _service.GetEnglishArticle(chineseArticle);
        }
    }
}

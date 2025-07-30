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
using QmtdltTools.Domain.Data;
using QmtdltTools.Service;

namespace QmtdltTools.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class VocabularyController : AbpController
    {
        private readonly VocabularyService _service;
        private readonly TranslationService _translationService;
        private readonly GuestQuotaLimitService _guestQuotaLimitService;
        public VocabularyController(VocabularyService service, TranslationService translationService, GuestQuotaLimitService guestQuotaLimitService)
        {
            _translationService = translationService;
            _service = service;
            _guestQuotaLimitService = guestQuotaLimitService;
        }
        [HttpGet("GetUserRecordsPage")]
        public async Task<Response<PageResult<VocabularyDto>>> GetUserRecordsPage(int pageindex,int pagesize)
        {
            return new Response<PageResult<VocabularyDto>>
            {
                data = await _service.GetPageByUserId(HttpContext.GetUserId(), pageindex, pagesize),
                code = 200,
            };
        }
        
        [HttpPost("MakeSentence")]
        public async Task<UserVocabulary?> MakeSentence([FromBody] MakeSentenceInputDto input)
        {
            return await _service.MakeSentence(input);
        }
        
        [HttpPost("AddVocabularyRecord")]
        public async Task<Response<bool>> AddVocabularyRecord(VocabularyRecord content)
        {
            try
            {
                content.CreateBy = HttpContext.GetUserId();
                content.CreateTime = DateTime.Now;

                await _service.AddRecord(content);

                return new Response<bool>
                {
                    data = true,
                    code = 200,
                };
            }
            catch (Exception e)
            {
                return new Response<bool>
                {
                    data = false,
                    code = 1,
                    message = e.Message
                };
            }
        }
        [HttpGet("Trans")]
        public async Task<VocabularyRecord?> Trans(string word)
        {
            string? uidStr = HttpContext.GetUserIdStr();           // 当前登录用户id

            if (await _guestQuotaLimitService.IsLimited(uidStr, "TranslateWords", ApplicationConst.QuotaTranslateWordsCnt))
            {
                //throw new Exception("上传电子书次数已达上限，请稍后再试");
                throw new QuotaLimitException("TranslateWords", "翻译词汇数已达上限，请注册或下月再试");
            }
            await _guestQuotaLimitService.AddUsage(uidStr, "TranslateWords");

            VocabularyRecord? findRes = await _translationService.Trans(0, 0,"", word,HttpContext.GetUserId());
            return findRes;
        }
        [HttpGet("TransOneBook")]
        public async Task TransOneBook()
        {
            await _translationService.TransOneBook(HttpContext.GetUserId());
        }
        [HttpGet("GetOneWord")]
        public async Task<VocabularyDto?> GetOneWord()
        {
            return await _service.GetOneWord(HttpContext.GetUserId());
        }
        [HttpPost("IgnoreInTimeRange")]
        public void IgnoreInTimeRange(Guid id)
        {
            _service.IgnoreInTimeRange(HttpContext.GetUserId(), id);
        }
    }
}

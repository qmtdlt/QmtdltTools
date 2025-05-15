using Volo.Abp.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using QmtdltTools.Service.Services;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Polly;
using QmtdltTools.Domain.Dtos;
using QmtdltTools.Extensions;

namespace QmtdltTools.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class VocabularyController : AbpController
    {
        private readonly VocabularyService _service;
        private readonly TranslationService _translationService;
        public VocabularyController(VocabularyService service, TranslationService translationService)
        {
            _translationService = translationService;
            _service = service;
        }
        [HttpGet("GetUserRecordsPage")]
        public async Task<Response<PageResult<VocabularyRecord>>> GetUserRecordsPage(int pageindex,int pagesize)
        {
            return new Response<PageResult<VocabularyRecord>>
            {
                data = await _service.GetPageByUserId(HttpContext.GetUserId(), pageindex, pagesize),
                code = 200,
            };
        }
        [HttpGet("GetBookRecordsPage")]
        public async Task<Response<PageResult<VocabularyRecord>>> GetBookRecordsPage(Guid bookId, int pageindex, int pagesize)
        {
            return new Response<PageResult<VocabularyRecord>>
            {
                data = await _service.GetPageByBookId(bookId, pageindex, pagesize),
                code = 200,
            };
        }
        [HttpPost("MakeSentence")]
        public async Task<VocabularyRecord?> MakeSentence([FromBody] MakeSentenceInputDto input)
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
            VocabularyRecord? findRes = await _translationService.Find(0, 0,"", word,HttpContext.GetUserId());
            return findRes;
        }
        [HttpGet("GetOneWord")]
        public async Task<VocabularyRecord?> GetOneWord()
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

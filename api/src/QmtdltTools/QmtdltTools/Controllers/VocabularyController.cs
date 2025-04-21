using Volo.Abp.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using QmtdltTools.Service.Services;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.Domain.Models;
using Microsoft.AspNetCore.Authorization;
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
        public VocabularyController(VocabularyService listenWriteService)
        {
            _service = listenWriteService;
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
    }
}

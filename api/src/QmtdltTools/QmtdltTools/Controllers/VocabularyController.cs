﻿using Volo.Abp.AspNetCore.Mvc;
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

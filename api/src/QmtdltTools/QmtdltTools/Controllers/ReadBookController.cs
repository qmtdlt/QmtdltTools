using Volo.Abp.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using QmtdltTools.Service.Services;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Polly;
using QmtdltTools.Domain.Dtos;
using QmtdltTools.Extensions;
using QmtdltTools.Domain.Data;
using QmtdltTools.Service;

namespace QmtdltTools.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReadBookController : AbpController
    {
        private readonly ExplainPhaseService _service;
        private readonly GuestQuotaLimitService _guestQuotaLimitService;
        public ReadBookController(ExplainPhaseService aiApiService, GuestQuotaLimitService guestQuotaLimitService)
        {
            _service = aiApiService;
            _guestQuotaLimitService = guestQuotaLimitService;
        }

        [HttpPost("GetNext")]
        public async Task<ExplainRecord?> GetNext([FromBody] ExplainRecord? input)
        {
            return await _service.GetNext(HttpContext.GetUserId());
        }
        [HttpPost("GetExplainResult")]
        public async Task<ExplainResultDto?> GetExplainResult([FromBody] ExplainPhaseInputDto input)
        {
            string? uidStr = HttpContext.GetUserIdStr();           // 当前登录用户id
            if (await _guestQuotaLimitService.IsLimited(uidStr, "ExplainParagraph", ApplicationConst.QuotaExplainParagraph))
            {
                //throw new Exception("上传电子书次数已达上限，请稍后再试");
                throw new QuotaLimitException("ExplainParagraph", "段落讲解次数已达上限，请注册或下月再试");
            }
            await _guestQuotaLimitService.AddUsage(uidStr, "ExplainParagraph");
            return await _service.GetExplainResult(input, HttpContext.GetUserId());
        }
    }
}

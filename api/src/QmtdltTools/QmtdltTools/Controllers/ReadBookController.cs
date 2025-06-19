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
    public class ReadBookController : AbpController
    {
        private readonly ExplainPhaseService _service;
        public ReadBookController(ExplainPhaseService aiApiService)
        {
            _service = aiApiService;
        }
       
        [HttpPost("GetExplainResult")]
        public async Task<ExplainResultDto?> GetExplainResult([FromBody] ExplainPhaseInputDto input)
        {
            return await _service.GetExplainResult(input);
        }
    }
}

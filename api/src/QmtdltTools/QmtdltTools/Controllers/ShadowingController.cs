using Volo.Abp.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using QmtdltTools.Service.Services;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using QmtdltTools.Domain.Data;
using QmtdltTools.Extensions;
using Autofac.Core;
using Polly;
using QmtdltTools.Domain.Dtos;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;

namespace QmtdltTools.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ShadowingController : AbpController
    {
        public ShadowingController()
        {
        }
        [HttpPost("CheckShadowing")]
        public async Task<PronunciationAssessmentResult?> CheckShadowing([FromForm] IFormFile audioFile,string reftext) // Added [FromForm]
        {
            var result = await MsTTSHelperRest.AssessPronunciationAsync(audioFile, reftext);
            Console.WriteLine(result);
            return result;
        }
    }
}

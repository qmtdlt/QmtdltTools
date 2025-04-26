using Volo.Abp.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using QmtdltTools.Service.Services;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using QmtdltTools.Domain.Data;
using QmtdltTools.Extensions;

namespace QmtdltTools.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ListenWriteController : AbpController
    {
        private readonly ListenWriteService _listenWriteService;
        public ListenWriteController(ListenWriteService listenWriteService)
        {
            _listenWriteService = listenWriteService;
        }
        [HttpGet("GetUserRecords")]
        public async Task<Response<PageResult<ListenWriteRecord>>> GetUserRecords(int pageindex,int pagesize)
        {
            return new Response<PageResult<ListenWriteRecord>>
            {
                data = await _listenWriteService.GetListByUserId(HttpContext.GetUserId(),pageindex, pagesize),
                code = 200,
            };
        }
        [HttpPost("AddLWRecord")]
        public async Task<Response<bool>> AddLWRecord(ListenWriteRecord content)
        {
            try
            {
                content.CreateBy = HttpContext.GetUserId();
                await _listenWriteService.AddRecord(content);

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

using Volo.Abp.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using QmtdltTools.Service.Services;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using QmtdltTools.Extensions;

namespace QmtdltTools.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ToDoController : AbpController
    {
        private readonly DayToDoService _dayToDoService;
        private readonly YearToDoService _yearToDoService;
        public ToDoController(YearToDoService yearToDoService, DayToDoService dayToDoService)
        {
            _dayToDoService = dayToDoService;
            _yearToDoService = yearToDoService;
        }
        [HttpPost("AddDayToDoItem")]
        public async Task<Response<bool>> AddDayToDoItem(string content)
        {
            
            return await _dayToDoService.AddItem(content, HttpContext.GetUserId());
        }
        [HttpPost("DeleteDayToDoItem")]
        public async Task DeleteDayToDoItem(DayToDo input)
        {
            await _dayToDoService.DeleteItem(input.Id);
        }

        [HttpPost("MarkAsComplete")]
        public async Task MarkAsComplete(DayToDo input)
        {
            await _dayToDoService.MarkAsComplete(input.Id);
        }
        [HttpGet("GetDayFinishedList")]
        public async Task<List<DayToDo>> GetDayFinishedList()
        {
            return await _dayToDoService.GetFinishedList(HttpContext.GetUserId());
        }
        [HttpGet("GetDayUnFinishedList")]
        public async Task<List<DayToDo>> GetDayUnFinishedList()
        {
            return await _dayToDoService.GetUnFinishedUnFinishedList(HttpContext.GetUserId());
        }
        [HttpGet("GetCurrentUnFinishedList")]
        public async Task<List<DayToDo>> GetCurrentUnFinishedList()
        {
            return await _dayToDoService.GetCurrentUnFinishedList(HttpContext.GetUserId());
        }
        [HttpPost("SetItemInCurrent")]
        public async Task SetItemInCurrent(DayToDo input)
        {
            await _dayToDoService.SetInCurrent(input.Id);
        }
        [HttpPost("SetItemOutCurrent")]
        public async Task SetItemOutCurrent(DayToDo input)
        {
            await _dayToDoService.SetOutCurrent(input.Id);
        }
    }
}

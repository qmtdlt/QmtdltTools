using Volo.Abp.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using QmtdltTools.Service.Services;

namespace QmtdltTools.Controllers
{
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
        public async Task AddDayToDoItem(string content)
        {
            await _dayToDoService.AddItem(content);
        }

        [HttpGet("GetDayUnFinishedList")]
        public async Task<List<Domain.Entitys.DayToDo>> GetDayUnFinishedList()
        {
            return await _dayToDoService.GetUnFinishedList();
        }
    }
}

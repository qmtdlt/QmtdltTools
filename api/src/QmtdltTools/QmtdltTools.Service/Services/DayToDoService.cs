using Microsoft.EntityFrameworkCore;
using QmtdltTools.EFCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Service.Services
{
    public class DayToDoService:ITransientDependency
    {
        private readonly DC _dc;
        public DayToDoService(DC dc)
        {
            _dc = dc;
        }

        public async Task AddItem(string content)
        {
            await _dc.DayToDos.AddAsync(new Domain.Entitys.DayToDo
            {
                Id = Guid.NewGuid(),
                Content = content,
                CreateTime = DateTime.Now
            });
            await _dc.SaveChangesAsync();
        }

        public async Task UpdateItem(Guid id, string content)
        {
            var item = await _dc.DayToDos.FindAsync(id);
            item.Content = content;
            item.UpdateTime = DateTime.Now;
            await _dc.SaveChangesAsync();
        }

        public async Task DeleteItem(Guid id)
        {
            var item = await _dc.DayToDos.FindAsync(id);
            _dc.DayToDos.Remove(item);
            await _dc.SaveChangesAsync();
        }

        public async Task<List<Domain.Entitys.DayToDo>> GetFinishedList()
        {
            return await _dc.DayToDos.Where(t=>t.IsFinish == true).ToListAsync();
        }
        public async Task<List<Domain.Entitys.DayToDo>> GetUnFinishedList()
        {
            return await _dc.DayToDos.Where(t => t.IsFinish != true).ToListAsync();
        }
    }
}

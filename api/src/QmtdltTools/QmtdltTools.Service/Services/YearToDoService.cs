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
    public class YearToDoService : ITransientDependency
    {
        private readonly DC _dc;
        public YearToDoService(DC dc)
        {
            _dc = dc;
        }
        public async Task AddItem(string content)
        {
            await _dc.YearToDos.AddAsync(new Domain.Entitys.YearToDo
            {
                Id = Guid.NewGuid(),
                Content = content,
                CreateTime = DateTime.Now
            });
            await _dc.SaveChangesAsync();
        }
        public async Task UpdateItem(Guid id, string content)
        {
            var item = await _dc.YearToDos.FindAsync(id);
            item.Content = content;
            item.UpdateTime = DateTime.Now;
            await _dc.SaveChangesAsync();
        }
        public async Task DeleteItem(Guid id)
        {
            var item = await _dc.YearToDos.FindAsync(id);
            _dc.YearToDos.Remove(item);
            await _dc.SaveChangesAsync();
        }
        public async Task<List<Domain.Entitys.YearToDo>> GetList()
        {
            return await _dc.YearToDos.ToListAsync();
        }
    }
}

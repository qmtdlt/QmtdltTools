using Microsoft.EntityFrameworkCore;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.Domain.Models;
using QmtdltTools.EFCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
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

        public async Task<Response<bool>> AddItem(string content, Guid? uid)
        {
            var list = await GetCurrentUnFinishedList(uid);
            if (list.Count >= 3)
            {
                return new Response<bool>
                {
                    data = false,
                    message = "当前待办事项已经有3个，无法再添加",
                    code = 500
                };
            }

            await _dc.DayToDos.AddAsync(new DayToDo
            {
                Id = Guid.NewGuid(),
                Content = content,
                CreateTime = DateTime.Now,
                CreateBy = uid
            });
            await _dc.SaveChangesAsync();
            return new Response<bool>
            {
                data = true
            };
        }

        public async Task UpdateItem(Guid id, string content)
        {
            var item = await _dc.DayToDos.FindAsync(id);
            item.Content = content;
            item.UpdateTime = DateTime.Now;
            _dc.Update(item);
            await _dc.SaveChangesAsync();
        }

        public async Task MarkAsComplete(Guid id)
        {
            var item = await _dc.DayToDos.FindAsync(id);
            if (null == item) return;

            item.IsFinish = true;
            item.UpdateTime = DateTime.Now;
            item.FinishTime = DateTime.Now;
            _dc.Update(item);
            await _dc.SaveChangesAsync();
        }

        public async Task DeleteItem(Guid id)
        {
            var item = await _dc.DayToDos.FindAsync(id);
            _dc.DayToDos.Remove(item);
            await _dc.SaveChangesAsync();
        }

        public async Task<List<DayToDo>> GetCurrentUnFinishedList(Guid? uid)
        {
            return await _dc.DayToDos
                .Where(t => t.IsFinish != true)             // 未完成
                .Where(t=>t.InCurrent == true)              // 且为当前待办
                .Where(t=>t.CreateBy == uid)
                .OrderBy(t => t.SortBy).ThenBy(t => t.UpdateTime).ToListAsync();
        }
        public async Task<List<DayToDo>> GetUnFinishedUnFinishedList(Guid? uid)
        {
            return await _dc.DayToDos
                .Where(t => t.IsFinish != true)
                .Where(t => t.InCurrent != true)
                .Where(t => t.CreateBy == uid)
                .OrderBy(t => t.SortBy).ThenBy(t => t.UpdateTime).ToListAsync();
        }
        public async Task<List<DayToDo>> GetFinishedList(Guid? uid)
        {
            return await _dc.DayToDos
                .Where(t=>t.IsFinish == true)
                .Where(t=>t.CreateBy == uid)
                .OrderByDescending(t => t.UpdateTime).ToListAsync();
        }

        public async Task SetInCurrent(Guid id)
        {
            var item = await _dc.DayToDos.FindAsync(id);
            if (null == item) return;
            item.InCurrent = true;
            item.UpdateTime = DateTime.Now;
            _dc.Update(item);
            await _dc.SaveChangesAsync();
        }

        public async Task SetOutCurrent(Guid id)
        {
            var item = await _dc.DayToDos.FindAsync(id);
            if (null == item) return;
            item.InCurrent = false;
            item.UpdateTime = DateTime.Now;
            _dc.Update(item);
            await _dc.SaveChangesAsync();
        }
    }
}

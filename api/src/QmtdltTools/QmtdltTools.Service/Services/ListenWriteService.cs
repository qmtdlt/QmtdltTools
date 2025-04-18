using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.EFCore;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Service.Services
{
    public class ListenWriteService:ITransientDependency
    {
        private readonly DC _dc;
        public ListenWriteService(DC dc)
        {
            _dc = dc;
        }

        public async Task AddRecord(ListenWriteRecord input)
        {
            input.Id = Guid.NewGuid();
            input.UpdateTime = DateTime.Now;
            await _dc.ListenWriteRecords.AddAsync(input);
            await _dc.SaveChangesAsync();
        }
        public async Task<List<ListenWriteRecord>> GetListByBookId(Guid BookId)
        {
            List<ListenWriteRecord> list = await _dc.ListenWriteRecords.Where(x => x.BookId == BookId).ToListAsync();
            return list;
        }
        public async Task<List<ListenWriteRecord>> GetListByUserId(Guid? uid)
        {
            List<ListenWriteRecord> list = await _dc.ListenWriteRecords.Where(x => x.CreateBy == uid).ToListAsync();
            return list;
        }
    }
}

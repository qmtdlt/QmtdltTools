using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QmtdltTools.Domain.Data;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.Domain.Models;
using QmtdltTools.EFCore;
using QmtdltTools.EFCore.Extensions;
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
            // if exists input.SentenceText return
            try
            {
                var entity = await _dc.ListenWriteRecords
                    .Where(x => x.SentenceText.Trim() == input.SentenceText.Trim())
                    .FirstOrDefaultAsync();
                if (entity != null)
                {
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            input.Id = Guid.NewGuid();
            input.UpdateTime = DateTime.Now;
            input.CreateTime = DateTime.Now;
            if (!string.IsNullOrEmpty(input.SentenceText))
            {
                input.Pronunciation = MsTTSHelperRest.GetSpeakStreamRest(input.SentenceText,ApplicationConst.DefaultVoiceName);
            }
            await _dc.ListenWriteRecords.AddAsync(input);
            await _dc.SaveChangesAsync();
        }
        public async Task<List<ListenWriteRecord>> GetListByBookId(Guid BookId)
        {
            List<ListenWriteRecord> list = await _dc.ListenWriteRecords.Where(x => x.BookId == BookId).ToListAsync();
            return list;
        }
        public async Task<PageResult<ListenWriteRecord>> GetListByUserId(Guid? uid,int pageIndex,int pageSize)
        {
            var page = await _dc.ListenWriteRecords.Where(x => x.CreateBy == uid)
                .OrderByDescending(t=>t.CreateTime).ToPageList(pageIndex,pageSize);
            return page;
        }
    }
}

using System;
using System.Collections.Concurrent;
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


        public async Task<ListenWriteRecord?> GetOneSentence(Guid? uid)
        {
            if (uid == null)
            {
                return null;
            }
            bool success = userViewWordIndexCache.TryGetValue(uid.Value, out int index);
            if (success)
            {
                index++;
                userViewWordIndexCache[uid.Value] = index;
            }
            else
            {
                index = 0;
                userViewWordIndexCache.TryAdd(uid.Value, index);
            }
            // 获取当前用户的忽略单词列表
            success = userIgnoreIdsCache.TryGetValue(uid.Value, out List<Guid> ignoreList);
            // 如果index大于表中单词数量，则重新开始
            int count = 0;
            if (success && ignoreList != null && ignoreList.Count > 0)
            {
                // 获取忽略之后的单词数量
                count = await _dc.ListenWriteRecords.Where(x => !ignoreList.Contains(x.Id)).CountAsync();
            }
            else
            {
                count = await _dc.ListenWriteRecords.CountAsync();
            }
            if (index >= count)
            {
                index = 0;
                userViewWordIndexCache[uid.Value] = index;
            }
            // 过滤掉忽略的单词，然后
            var query = _dc.ListenWriteRecords.AsQueryable();
            if (success && ignoreList != null && ignoreList.Count > 0)
            {
                query = query.Where(x => !ignoreList.Contains(x.Id));
            }
            var word = await query
                .Skip(index)
                .Take(1)
                .FirstOrDefaultAsync();
            return word;
        }

        public void IgnoreInTimeRange(Guid? uid, Guid vocRecordId)
        {
            if (uid != null)
            {
                // 如果未缓存，则添加
                if (!userIgnoreIdsCache.ContainsKey(uid.Value))
                {
                    userIgnoreIdsCache.TryAdd(uid.Value, new List<Guid>());
                }
                // 获取当前用户的忽略单词列表
                List<Guid> ignoreList = userIgnoreIdsCache[uid.Value];
                // 如果列表中不存在该单词，则添加
                if (!ignoreList.Contains(vocRecordId))
                {
                    ignoreList.Add(vocRecordId);
                    userIgnoreIdsCache[uid.Value] = ignoreList;
                }
            }
        }
        // 缓存用户id对应当前查看的单词（第N个）
        static ConcurrentDictionary<Guid, int> userViewWordIndexCache = new ConcurrentDictionary<Guid, int>();
        // 缓存用户id对应的忽略单词id列表
        static ConcurrentDictionary<Guid, List<Guid>> userIgnoreIdsCache = new ConcurrentDictionary<Guid, List<Guid>>();
    }
}

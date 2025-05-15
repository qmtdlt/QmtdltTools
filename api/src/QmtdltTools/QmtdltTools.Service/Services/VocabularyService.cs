using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QmtdltTools.Domain.Dtos;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.Domain.Models;
using QmtdltTools.EFCore;
using QmtdltTools.EFCore.Extensions;
using QmtdltTools.Service.Utils;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Service.Services
{
    public class VocabularyService:ITransientDependency
    {
        private readonly DC _dc;
        private readonly AiApiService _aiApiService;
        public VocabularyService(DC dc, AiApiService aiApiService)
        {
            _dc = dc;
            _aiApiService = aiApiService;
        }

        public async Task AddRecord(VocabularyRecord input)
        {
            input.Id = Guid.NewGuid();
            input.UpdateTime = DateTime.Now;
            await _dc.VocabularyRecords.AddAsync(input);
            await _dc.SaveChangesAsync();
        }
        public async Task<PageResult<VocabularyRecord>> GetPageByBookId(Guid BookId,int pageIndex,int pageSize)
        {
            PageResult<VocabularyRecord> page = await _dc.VocabularyRecords
                //.Where(x => x.BookId == BookId)
                .OrderByDescending(t=>t.CreateTime)
                .ToPageList(pageIndex, pageSize);
            return page;
        }
        public async Task<PageResult<VocabularyRecord>> GetPageByUserId(Guid? uid, int pageIndex, int pageSize)
        {
            var page = await _dc.VocabularyRecords.Where(x => x.CreateBy == uid)
                .OrderByDescending(t=>t.CreateTime)
                .ToPageList(pageIndex, pageSize);
            return page;
        }
        public async Task<List<VocabularyRecord>> GetListByBookId(Guid BookId)
        {
            List<VocabularyRecord> list = await _dc.VocabularyRecords
                //.Where(x => x.BookId == BookId)
                .OrderByDescending(t=>t.CreateTime).ToListAsync();
            return list;
        }
        public async Task<List<VocabularyRecord>> GetListByUserId(Guid? uid)
        {
            List<VocabularyRecord> list = await _dc.VocabularyRecords.Where(x => x.CreateBy == uid)
                .OrderByDescending(t=>t.CreateTime).ToListAsync();
            return list;
        }

        public async Task<VocabularyRecord> MakeSentence(MakeSentenceInputDto input)
        {
            VocabularyRecord? entity = await _dc.VocabularyRecords.Where(t=>t.Id == input.Id).FirstOrDefaultAsync();
            if (entity != null)
            {
                entity.SentenceYouMade = input.Sentence;
                SentenceEvaluateDto? dtores = await _aiApiService.GetSentenctevaluate(input.Sentence, entity.WordText);
                entity.IfUsageCorrect = dtores.IfUsageCorrect;
                entity.IncorrectReason = dtores.IncorrectReason;
                entity.CorrectSentence = dtores.CorrectSentence;
                _dc.VocabularyRecords.Update(entity);
                await _dc.SaveChangesAsync();
            }
            return entity; 
        }

        public async Task<VocabularyRecord?> GetOneWord(Guid? uid)
        {
            if(uid == null)
            {
                return null;
            }
            bool success = userViewWordIndexCache.TryGetValue(uid.Value, out int index);
            if(success)
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
                count = await _dc.VocabularyRecords.Where(x => !ignoreList.Contains(x.Id)).CountAsync();
            }
            else
            {
                count = await _dc.VocabularyRecords.CountAsync();
            }
            if (index >= count)
            {
                index = 0;
                userViewWordIndexCache[uid.Value] = index;
            }
            // 过滤掉忽略的单词，然后
            var query = _dc.VocabularyRecords.AsQueryable();
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
        static ConcurrentDictionary<Guid,int> userViewWordIndexCache = new ConcurrentDictionary<Guid, int>();
        // 缓存用户id对应的忽略单词id列表
        static ConcurrentDictionary<Guid, List<Guid>> userIgnoreIdsCache = new ConcurrentDictionary<Guid, List<Guid>>();
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
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
        public async Task UpdateRecord(VocabularyRecord input)
        {
            input.UpdateTime = DateTime.Now;
            _dc.VocabularyRecords.Update(input);
            await _dc.SaveChangesAsync();
        }
        public async Task AddRecord(VocabularyRecord input)
        {
            input.Id = Guid.NewGuid();
            input.UpdateTime = DateTime.Now;
            await _dc.VocabularyRecords.AddAsync(input);
            await _dc.SaveChangesAsync();

            // 如果是添加单词，则添加到用户单词表中
            UserVocabulary userVocabulary = new UserVocabulary
            {
                Id = Guid.NewGuid(),
                VocabularyId = input.Id,
                CreateBy = input.CreateBy,
                CreateTime = DateTime.Now
            };
            await _dc.UserVocabularies.AddAsync(userVocabulary);
            await _dc.SaveChangesAsync();
        }
        
        public async Task<PageResult<VocabularyDto>> GetPageByUserId(Guid? uid, int pageIndex, int pageSize)
        {
            // UserVocabularies leftjoin VocabularyRecords，on 条件 UserVocabularies 的 VocabularyId 等于 VocabularyRecords 的 Id
            var query = from uv in _dc.UserVocabularies.Where(t=>t.CreateBy == uid)
                        join vr in _dc.VocabularyRecords
                        on uv.VocabularyId equals vr.Id into vrGroup
                        from vr in vrGroup.DefaultIfEmpty() // LEFT JOIN
                        select new VocabularyDto
                        {
                            Id = uv.Id,
                            VocabularyId = uv.VocabularyId,
                            SentenceYouMade = uv.SentenceYouMade,
                            SentencePronunciation = uv.SentencePronunciation,
                            IfUsageCorrect = uv.IfUsageCorrect,
                            IncorrectReason = uv.IncorrectReason,
                            CorrectSentence = uv.CorrectSentence,
                            CreateTime = uv.CreateTime,

                            WordText = vr != null ? vr.WordText : null,
                            WordPronunciation = vr != null ? vr.WordPronunciation : null,
                            AIExplanation = vr != null ? vr.AIExplanation : null,
                            //Pronunciation = vr != null ? vr.Pronunciation : null,
                            AITranslation = vr != null ? vr.AITranslation : null
                        };
            var page = await query.OrderByDescending(t => t.CreateTime).ToPageList(pageIndex, pageSize);

            foreach (var item in page.PageList)
            {
                item.WordText += "测试cicd";
            }

            return page;
        }

        public async Task<UserVocabulary> MakeSentence(MakeSentenceInputDto input)
        {
            UserVocabulary? entity = await _dc.UserVocabularies.Where(t=>t.Id == input.Id).FirstOrDefaultAsync();
            if (entity != null)
            {
                entity.SentenceYouMade = input.Sentence;
                SentenceEvaluateDto? dtores = await _aiApiService.GetSentenctevaluate(input.Sentence, input.WordText);
                entity.IfUsageCorrect = dtores.IfUsageCorrect;
                entity.IncorrectReason = dtores.IncorrectReason;
                entity.CorrectSentence = dtores.CorrectSentence;
                _dc.UserVocabularies.Update(entity);
                await _dc.SaveChangesAsync();
            }
            return entity; 
        }
        async Task<List<VocabularyDto>> GetListByUid(Guid? uid)
        {
            // UserVocabularies leftjoin VocabularyRecords，on 条件 UserVocabularies 的 VocabularyId 等于 VocabularyRecords 的 Id
            var query = from uv in _dc.UserVocabularies.Where(t => t.CreateBy == uid)
                        join vr in _dc.VocabularyRecords
                        on uv.VocabularyId equals vr.Id into vrGroup
                        from vr in vrGroup.DefaultIfEmpty() // LEFT JOIN
                        select new VocabularyDto
                        {
                            Id = uv.Id,
                            VocabularyId = uv.VocabularyId,
                            SentenceYouMade = uv.SentenceYouMade,
                            SentencePronunciation = uv.SentencePronunciation,
                            IfUsageCorrect = uv.IfUsageCorrect,
                            IncorrectReason = uv.IncorrectReason,
                            CorrectSentence = uv.CorrectSentence,
                            CreateTime = uv.CreateTime,
                            WordText = vr != null ? vr.WordText : null,
                            WordPronunciation = vr != null ? vr.WordPronunciation : null,
                            AIExplanation = vr != null ? vr.AIExplanation : null,
                            //Pronunciation = vr != null ? vr.Pronunciation : null,
                            AITranslation = vr != null ? vr.AITranslation : null
                        };
            var list = await query.OrderByDescending(t => t.CreateTime).ToListAsync();
            return list;
        }

        public void UsrTransNew(Guid uid)
        {
            userViewWordIndexCache.AddOrUpdate(uid, 0, (key, oldValue) => 0);
        }
        public async Task<VocabularyDto?> GetOneWord(Guid? uid)
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
            var list = await GetListByUid(uid);
            if (success && ignoreList != null && ignoreList.Count > 0)
            {
                // 获取忽略之后的单词数量
                count = list.Where(x => !ignoreList.Contains(x.Id)).Count();
            }
            else
            {
                count = list.Count;
            }
            if (index >= count)
            {
                index = 0;
                userViewWordIndexCache[uid.Value] = index;
            }
            // 过滤掉忽略的单词，然后
            if (success && ignoreList != null && ignoreList.Count > 0)
            {
                list = list.Where(x => !ignoreList.Contains(x.Id)).ToList();
            }
            var word = list
                .Skip(index)
                .Take(1)
                .FirstOrDefault();
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

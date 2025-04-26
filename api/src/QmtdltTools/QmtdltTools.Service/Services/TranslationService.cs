using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QmtdltTools.Domain.Data;
using QmtdltTools.Domain.Dtos;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.EFCore;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Service.Services
{
    public class TranslationService:ITransientDependency
    {
        private readonly DC _dc;
        private readonly AiApiService _aiApiService;
        public TranslationService(DC dc, AiApiService aiApiService)
        {
            _dc = dc;
            _aiApiService = aiApiService;
        }
        public async Task AddRecord(VocabularyRecord record)
        {
            record.Id = Guid.NewGuid();
            record.CreateTime = DateTime.Now;
            await _dc.VocabularyRecords.AddAsync(record);
            await _dc.SaveChangesAsync();
        }
        public async Task<VocabularyRecord?> Find(Guid bookId,int pIndex,int sIndex,string sentence,string word,Guid? uid)
        {
            var entity = await _dc.VocabularyRecords
                .Where(x=>!string.IsNullOrEmpty(x.WordText)
                          && !string.IsNullOrEmpty(word)
                          && x.WordText.ToLower().Trim() == word.ToLower().Trim())
                .FirstOrDefaultAsync();
            if (entity == null)
            {
                TranslateDto? res = await _aiApiService.GetTranslateResult(word);       // 翻译
                if (res != null)
                {
                    entity = new Domain.Entitys.VocabularyRecord
                    {
                        BookId = bookId,
                        WordText = word,
                        WordPronunciation = MsTTSHelperRest.GetSpeakStreamRest(word,ApplicationConst.DefaultVoiceName), // 单词配音
                        Pronunciation = res.VoiceBuffer,
                        AIExplanation = res.Explanation,
                        AITranslation = res.Translation,
                        CreateBy = uid
                    };
                    await AddRecord(entity);
                    return entity;
                }
            }
            else
            {
                return entity;
            }
            return null;
        }
        public Task GetListBookId(Guid bookId)
        {
            var list = _dc.VocabularyRecords.Where(x => x.BookId == bookId).ToList();
            return Task.FromResult(list);
        }
    }
}

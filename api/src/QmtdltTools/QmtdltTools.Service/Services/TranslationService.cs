using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QmtdltTools.Domain.Data;
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
        public async Task<VocabularyRecord?> Find(Guid bookId,int pIndex,int sIndex,string word)
        {
            var entity = await _dc.VocabularyRecords
                .Where(x => x.BookId == bookId 
                && !string.IsNullOrEmpty(x.WordText) 
                && (x.WordText.Contains(word) || word.Contains(x.WordText)))
                .FirstOrDefaultAsync();
            var res = await _aiApiService.GetTranslateResult(word);
            if (res != null)
            {
                entity = new Domain.Entitys.VocabularyRecord
                {
                    BookId = bookId,
                    WordText = word,
                    WordPronunciation = TTSHelperRest.GetSpeakStreamRest(word,ApplicationConst.DefaultVoiceName),
                    Pronunciation = res.VoiceBuffer,
                    AIExplanation = res.Explanation,
                    AITranslation = res.Translation
                };
                await AddRecord(entity);
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

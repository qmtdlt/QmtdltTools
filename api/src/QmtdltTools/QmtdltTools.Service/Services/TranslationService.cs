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
        private readonly VocabularyService _vocabularyService;
        public TranslationService(DC dc, AiApiService aiApiService, VocabularyService vocabularyService)
        {
            _dc = dc;
            _aiApiService = aiApiService;
            _vocabularyService = vocabularyService;
        }
        public async Task<VocabularyRecord?> Trans(int pIndex, int sIndex, string sentence, string word, Guid? uid)
        {
            var entity = await _dc.VocabularyRecords
                .Where(x => !string.IsNullOrEmpty(x.WordText)
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
                        WordText = word,
                        WordPronunciation = MsTTSHelperRest.GetSpeakStreamRest(word, ApplicationConst.DefaultVoiceName), // 单词配音
                        Pronunciation = res.VoiceBuffer,
                        AIExplanation = res.Explanation,
                        AITranslation = res.Translation,
                        CreateBy = uid
                    };
                    await _vocabularyService.AddRecord(entity);
                    return entity;
                }
            }
            else
            {
                var userVoclbular = _dc.UserVocabularies.Where(t => t.VocabularyId == entity.Id && t.CreateBy == uid).FirstOrDefault();     // 查找当前用户是否有该单词
                if (null == userVoclbular)
                {
                    userVoclbular = new UserVocabulary
                    {
                        VocabularyId = entity.Id,
                        CreateBy = uid,
                        CreateTime = DateTime.Now
                    };
                    // 存储 userVoclbular
                    await _dc.UserVocabularies.AddAsync(userVoclbular);
                    await _dc.SaveChangesAsync();
                }
                return entity;
            }
            return null;
        }
    }
}

using Microsoft.EntityFrameworkCore;
using QmtdltTools.Domain.Data;
using QmtdltTools.Domain.Dtos;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.EFCore;
using QmtdltTools.Service.Utils;
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
            else if(entity.Pronunciation == null || entity.WordPronunciation == null
                || entity.Pronunciation.Length == 0 || entity.WordPronunciation.Length == 0)
            {
                TranslateDto? res = await _aiApiService.GetTranslateResult(word);       // 翻译
                if (res != null)
                {
                    entity.WordPronunciation = MsTTSHelperRest.GetSpeakStreamRest(word, ApplicationConst.DefaultVoiceName); // 单词配音
                    entity.Pronunciation = res.VoiceBuffer;
                    entity.AIExplanation = res.Explanation;
                    entity.AITranslation = res.Translation;

                    await _vocabularyService.UpdateRecord(entity);
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
            if(uid != null)
            {
                _vocabularyService.UsrTransNew(uid.Value);
            }
            return null;
        }

        public async Task TransOneBook(Guid? uid)
        {
            try
            {
                var bookId = Guid.Parse("08dd941a-332a-4c05-8192-21e288595e6b");
                //var book = await _epubManageService.GetBookById(bookId);            // read book info from database

                var book = await _dc.EBooks.Where(t => t.Id == bookId).FirstOrDefaultAsync();

                var ebook = EpubHelper.GetEbook(book.BookPath, out string message);     // get ebook
                var plist = EpubHelper.PrepareAllPragraphs(ebook);     // analyse book and get all pragraphs
                foreach (Domain.Models.MyPragraph item in plist)
                {
                    var words = item.PragraphText.Split(" ");
                    foreach (var word in words)
                    {
                        var tword = word.ToLower().Trim();
                        if (!string.IsNullOrEmpty(tword))
                        {
                            await Trans(0, 0, "", tword, uid);
                            Console.WriteLine($"tword trans finished:{tword}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return;
            }
        }
    }
}

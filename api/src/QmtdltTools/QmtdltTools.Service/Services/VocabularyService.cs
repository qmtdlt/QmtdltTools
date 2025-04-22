using System;
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
            PageResult<VocabularyRecord> page = await _dc.VocabularyRecords.Where(x => x.BookId == BookId)
                .ToPageList(pageIndex, pageSize);
            return page;
        }
        public async Task<PageResult<VocabularyRecord>> GetPageByUserId(Guid? uid, int pageIndex, int pageSize)
        {
            var page = await _dc.VocabularyRecords.Where(x => x.CreateBy == uid)
                .ToPageList(pageIndex, pageSize);
            return page;
        }
        public async Task<List<VocabularyRecord>> GetListByBookId(Guid BookId)
        {
            List<VocabularyRecord> list = await _dc.VocabularyRecords.Where(x => x.BookId == BookId).ToListAsync();
            return list;
        }
        public async Task<List<VocabularyRecord>> GetListByUserId(Guid? uid)
        {
            List<VocabularyRecord> list = await _dc.VocabularyRecords.Where(x => x.CreateBy == uid).ToListAsync();
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
    }
}

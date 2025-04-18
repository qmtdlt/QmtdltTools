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
    public class TranslationService:ITransientDependency
    {
        private readonly DC _dc;
        public TranslationService(DC dc)
        {
            _dc = dc;
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
                && x.PIndex == pIndex 
                && !string.IsNullOrEmpty(x.WordText) 
                && (x.WordText.Contains(word) || word.Contains(x.WordText)))
                .FirstOrDefaultAsync();
            return entity;
        }
        public Task GetListBookId(Guid bookId)
        {
            var list = _dc.VocabularyRecords.Where(x => x.BookId == bookId).ToList();
            return Task.FromResult(list);
        }
    }
}

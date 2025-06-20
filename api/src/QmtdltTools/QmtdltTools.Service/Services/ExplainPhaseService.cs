using Microsoft.EntityFrameworkCore;
using QmtdltTools.Domain.Dtos;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.EFCore;
using QmtdltTools.Service.Utils;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Service.Services
{
    public class ExplainPhaseService:IScopedDependency
    {

        private readonly AiApiService _aiApiService;
        private readonly DC _dc;
        public ExplainPhaseService(AiApiService aiApiService, DC dc)
        {
            _aiApiService = aiApiService;
            _dc = dc;
        }

        static ConcurrentDictionary<Guid, DateTime> UidReplayTimeDic = new ConcurrentDictionary<Guid, DateTime>();

        public async Task<ExplainRecord?> GetNext(Guid? uid)
        {
            if(uid != null)
            {
                bool success = UidReplayTimeDic.TryGetValue(uid.Value, out DateTime dateTime);
                if(success)
                {
                    var findEntity = await _dc.Set<ExplainRecord>()
                        .Where(t => t.CreateBy == uid)
                        .OrderBy(t => t.CreateTime)
                        .Where(t => t.CreateTime > dateTime).FirstOrDefaultAsync();
                    if (findEntity != null)
                    {
                        return findEntity;
                    }
                    else
                    {
                        return await _dc.Set<ExplainRecord>()
                        .Where(t => t.CreateBy == uid)
                        .OrderBy(t => t.CreateTime).FirstOrDefaultAsync();
                    }
                }
                else
                {
                    return await _dc.Set<ExplainRecord>()
                        .Where(t => t.CreateBy == uid)
                        .OrderBy(t => t.CreateTime).FirstOrDefaultAsync();
                }
            }
            return null;
        }
        public async Task<ExplainResultDto?> GetExplainResult(ExplainPhaseInputDto input,Guid? uid)
        {
            var findEntity = await _dc.Set<ExplainRecord>().Where(t => t.Phase == input.Phase).FirstOrDefaultAsync();

            if(findEntity != null)
            {
                //_ = ExplainNext(input, uid);
                return new ExplainResultDto
                {
                    Explanation = findEntity.Explanation,
                    VoiceBuffer = findEntity.VoiceBuffer
                };
            }
            else
            {
                var result = await _aiApiService.GetExplainResult(input.Phase);
                if(result != null)
                {
                    ExplainRecord addItem = new ExplainRecord
                    {
                        Id = Guid.NewGuid(),
                        BookId = input.bookId,
                        PhaseIndex = input.PhaseIndex,
                        Phase = input.Phase,
                        Explanation = result.Explanation,
                        VoiceBuffer = result.VoiceBuffer,
                        CreateTime = DateTime.Now,
                        CreateBy = uid
                    };
                    await _dc.Set<ExplainRecord>().AddAsync(addItem);
                    await _dc.SaveChangesAsync();
                }

                //_ = ExplainNext(input,uid);

                return result;
            }
        }

        private async Task ExplainNext(ExplainPhaseInputDto input,Guid? uid)
        {
            try
            {
                var book = await _dc.EBooks.Where(t => t.Id == input.bookId).FirstOrDefaultAsync();
                
                var ebook = EpubHelper.GetEbook(book.BookPath, out string message);     // get ebook
                var plist = EpubHelper.PrepareAllPragraphs(ebook);     // analyse book and get all pragraphs

                input.PhaseIndex = input.PhaseIndex + 1;
                if (input.PhaseIndex < plist.Count)
                {
                    input.Phase = plist[input.PhaseIndex].PragraphText;

                    // 
                    var findEntity = await _dc.Set<ExplainRecord>().Where(t => t.Phase == input.Phase).FirstOrDefaultAsync();
                    if(findEntity != null)
                    {
                        return;         // 下一段已有
                    }

                    var result = await _aiApiService.GetExplainResult(input.Phase);
                    if (result != null)
                    {
                        ExplainRecord addItem = new ExplainRecord
                        {
                            Id = Guid.NewGuid(),
                            BookId = input.bookId,
                            PhaseIndex = input.PhaseIndex,
                            Phase = input.Phase,
                            Explanation = result.Explanation,
                            VoiceBuffer = result.VoiceBuffer,
                            CreateTime = DateTime.Now,
                            CreateBy = uid
                        };
                        await _dc.Set<ExplainRecord>().AddAsync(addItem);
                        await _dc.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }
    }
}

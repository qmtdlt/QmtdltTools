using Microsoft.EntityFrameworkCore;
using QmtdltTools.Domain.Dtos;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.EFCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public async Task<ExplainResultDto?> GetExplainResult(ExplainPhaseInputDto input)
        {
            var findEntity = await _dc.Set<ExplainRecord>().Where(t => t.BookId == input.bookId && t.PhaseIndex == input.PhaseIndex).FirstOrDefaultAsync();

            if(findEntity != null)
            {
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
                        CreateTime = DateTime.Now
                    };
                    await _dc.Set<ExplainRecord>().AddAsync(addItem);
                    await _dc.SaveChangesAsync();
                }
                return result;
            }
        }
    }
}

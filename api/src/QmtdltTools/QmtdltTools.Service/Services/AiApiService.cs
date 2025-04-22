using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QmtdltTools.Domain.Dtos;
using QmtdltTools.Domain.Enums;
using QmtdltTools.Service.Utils;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Service.Services
{
    public class AiApiService:ISingletonDependency
    {
        private AIFactorys _AIFactorys;
        public AiApiService()
        {
            _AIFactorys = AIFactorys.QIANWEN;
        }
        public async Task<TranslateDto> GetTranslateResult(string word)
        {
            switch (_AIFactorys)
            {
                case AIFactorys.GROK:
                    return await GrokRestHelper.GetTranslateResult(word);
                case AIFactorys.GEMINI:
                    return await GeminiRestHelper.GetTranslateResult(word);
                case AIFactorys.DOUBAO:
                    return await DouBaoRestHelper.GetTranslateResult(word);
                case AIFactorys.QIANWEN:
                    return await QianWenRestHelper.GetTranslateResult(word);
                default:
                    return await QianWenRestHelper.GetTranslateResult(word);
            }
        }
        public async Task<SentenceEvaluateDto> GetSentenctevaluate(string sentence, string word)
        {
            switch (_AIFactorys)
            {
                case AIFactorys.GROK:
                    return await GrokRestHelper.GetSentenctevaluate(sentence, word);
                case AIFactorys.GEMINI:
                    return await GeminiRestHelper.GetSentenctevaluate(sentence, word);
                case AIFactorys.DOUBAO:
                    return await DouBaoRestHelper.GetSentenctevaluate(sentence, word);
                case AIFactorys.QIANWEN:
                    return await QianWenRestHelper.GetSentenctevaluate(sentence, word);
                default:
                    return await QianWenRestHelper.GetSentenctevaluate(sentence, word);
            }
        }
    }
}

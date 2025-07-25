using QmtdltTools.Domain.Entitys;
using QmtdltTools.WPF.Dto;
using QmtdltTools.WPF.IServices;
using QmtdltTools.WPF.Utils;
using QmtdltTools.WPF.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.WPF.Services
{
    public class TransService:ITransientDependency
    {
        ISubtitleService _subtitleService;
        public TransService(ISubtitleService subtitleService)
        {
            _subtitleService = subtitleService;
        }

        public async Task Trans(string selectedText)
        {
            var dialogRes = MessageBox.Show($"是否翻译选中的内容：{selectedText}", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            if(dialogRes == MessageBoxResult.OK)
            {
                //VocabularyRecord? findRes = await _translationService.Trans(0, 0, "", selectedText, Guid.Parse("08dd7e88-9af1-4775-8a21-554610976784"));
                VocabularyRecordDto? res = await RestHelper.Trans(selectedText);

                VocabularyRecord findRes = new VocabularyRecord
                {
                    WordText = res.wordText,
                    WordPronunciation = res.wordPronunciation.Base64ToBytes(),
                    //Pronunciation = res.pronunciation.Base64ToBytes(),
                    AIExplanation = res.aiExplanation,
                    AITranslation = res.aiTranslation
                };

                if (findRes != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var wd = App.Get<TranslateResultWindow>();
                        wd.setData(findRes);

                        _subtitleService.Pause();
                        
                        wd.ShowDialog();

                        _subtitleService.Resume();
                    });
                }
            }
        }
    }
}

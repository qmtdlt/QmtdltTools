using System.Threading.Tasks;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using QmtdltTools.Avaloina.Dto;
using QmtdltTools.Avaloina.Utils;
using QmtdltTools.Avaloina.Views;
using QmtdltTools.Domain.Entitys;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Avaloina.Services;

public class TransRestService:ITransientDependency
{
    public TransRestService()
    {
    }

    public async Task Trans(string selectedText)
    {
        
        var box = MessageBoxManager
            .GetMessageBoxStandard("提示", $"是否翻译选中的内容：{selectedText}",
                ButtonEnum.YesNo);

        ButtonResult dialogRes = await box.ShowAsync();
        // var dialogRes = MessageBox.Show($"是否翻译选中的内容：{selectedText}", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Information);
        if(dialogRes == ButtonResult.Yes)
        {
            VocabularyRecordDto? res = await RestHelper.Trans(selectedText);

            VocabularyRecord findRes = new VocabularyRecord
            {
                WordText = res.wordText,
                WordPronunciation = res.wordPronunciation.Base64ToBytes(),
                AIExplanation = res.aiExplanation,
                AITranslation = res.aiTranslation
            };

            if (findRes != null)
            {
                // MessageBoxManager.GetMessageBoxStandard("提示", findRes.AIExplanation, ButtonEnum.Ok).ShowAsync();
                //Application.Current.Dispatcher.Invoke(() =>
                //{
                var wd = App.Get<TranslateResultWindow>();
                wd.setData(findRes);
                wd.Show();
                //});
            }
        }
    }
}
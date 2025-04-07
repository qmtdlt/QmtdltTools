using System.Formats.Tar;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using QmtdltTools.Service.Services;
using QmtdltTools.Service.Utils;
using Volo.Abp.AspNetCore.SignalR;

namespace QmtdltTools.Hubs;

public class BookContentHub:AbpHub
{
    private readonly EpubManageService _epubManageService;
    public BookContentHub(EpubManageService epubManageService)
    {
        _epubManageService = epubManageService;
    }
    public async Task StartReadTask(Guid bookId)
    {
        var book = await _epubManageService.GetBookById(bookId);
        if(book == null)
        {
            await Clients.All.SendAsync("onShowErrMsg", "δ�ҵ�Id��Ӧ�ĵ�����");
            return;
        }
        if(File.Exists(book.BookPath))
        {
            var ebook = EpubHelper.GetEbook(book.BookPath,out string message);
            if(ebook == null)
            {
                await Clients.All.SendAsync("onShowErrMsg", message);
            }
            else
            {
                int count = ebook.ReadingOrder.Count;
                int position = 0;
                while (position < count)
                {
                    var item = ebook.ReadingOrder[position];

                    if (!string.IsNullOrEmpty(item.Content))
                    {
                        // ʹ��������ʽ���� HTML ��ǩ���õ����ı�
                        string plainText = Regex.Replace(item.Content, "<[^>]+>", " ");
                        // �ɽ�һ�����ı�����������ȥ������ո�
                        plainText = Regex.Replace(plainText, "\\s+", " ").Trim();

                        var buffer = EpubHelper.GetSpeakStream(plainText);

                        await Clients.All.SendAsync("onPlayVoiceBuffer", buffer);                   // send audio buffer
                        await Clients.All.SendAsync("onShowReadingText", plainText);                // send text
                    }

                    await Task.Delay(10000);
                    position++;
#if DEBUG
                    if(position > 5)
                    return;
#endif
                }
            }
        }
        else
        {
            await Clients.All.SendAsync("onShowErrMsg", "�����鲻����");
        }
    }

}
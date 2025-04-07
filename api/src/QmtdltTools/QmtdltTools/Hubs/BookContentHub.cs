using System.Collections.Concurrent;
using System.Formats.Tar;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using QmtdltTools.Service.Services;
using QmtdltTools.Service.Utils;
using VersOne.Epub;
using Volo.Abp.AspNetCore.SignalR;

namespace QmtdltTools.Hubs;

public class BookContentHub:AbpHub
{
    private readonly EpubManageService _epubManageService;
    public BookContentHub(EpubManageService epubManageService)
    {
        _epubManageService = epubManageService;
    }

    public override Task OnConnectedAsync()
    {
        Console.WriteLine("on connected");
        var connectionId = Context.ConnectionId;
        connectionStatusCache.AddOrUpdate(connectionId, true, (connectionId, old) => { return true; }); // �������״̬������
        return base.OnConnectedAsync();
    }
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine("on disconnected");
        var connectionId = Context.ConnectionId;
        connectionStatusCache.AddOrUpdate(connectionId, false, (connectionId, old) => { return false; }); // ��������״̬������
        return base.OnDisconnectedAsync(exception);
    }

    static ConcurrentDictionary<Guid,int> bookReadPosition = new ConcurrentDictionary<Guid,int>();
    static ConcurrentDictionary<string,bool> connectionStatusCache = new ConcurrentDictionary<string,bool>();
    public async Task StartReadTask(Guid bookId)
    {
        var connectionId = Context.ConnectionId;            // connectionId

        var book = await _epubManageService.GetBookById(bookId);            // ��ѯ���ݿ⣬��ȡ book ����
        if(book == null)
        {
            await Clients.All.SendAsync("onShowErrMsg", "δ�ҵ�Id��Ӧ�ĵ�����");
            return;
        }
        if(File.Exists(book.BookPath))                                      // ���������ļ��Ƿ����
        {
            var ebook = EpubHelper.GetEbook(book.BookPath,out string message);  // �������ϵĵ�������ص��ڴ���
            if (ebook == null)
            {
                await Clients.All.SendAsync("onShowErrMsg", message);           // ǰ����ʾ������Ϣ
            }
            else
            {
                _ = StartReadingTask(connectionId, ebook, bookId);        // �����Ķ�����
            }
        }
        else
        {
            await Clients.All.SendAsync("onShowErrMsg", "�����鲻����");
        }
    }

    async Task StartReadingTask(string connectionId, EpubBook ebook,Guid bookId)
    {
        int count = ebook.ReadingOrder.Count;                                       // ��ȡ��������½���
        bool success = bookReadPosition.TryGetValue(bookId, out int position);
        if (!success)
        {
            position = 0;
            // ���� bookReadPosition �ֵ�
            bookReadPosition.TryAdd(bookId, position);
        }
        while (position < count && connectionStatusCache[connectionId])
        {
            var item = ebook.ReadingOrder[position];

            if (!string.IsNullOrEmpty(item.Content))
            {
                // ʹ��������ʽ���� HTML ��ǩ���õ����ı�
                string plainText = Regex.Replace(item.Content, "<[^>]+>", " ");
                // �ɽ�һ�����ı�����������ȥ������ո�
                plainText = Regex.Replace(plainText, "\\s+", " ").Trim();

                var buffer = EpubHelper.GetSpeakStream(plainText);

                bookIsReading.AddOrUpdate(bookId, true, (bookId, old) => { return true; });

                await Clients.All.SendAsync("onPlayVoiceBuffer", buffer);                   // send audio buffer, this will be base64 string on client side
                await Clients.All.SendAsync("onShowReadingText", plainText);                // send text
                                                                                            // ֪ͨǰ�� dowork
                while (bookIsReading[bookId])
                {
                    await Task.Delay(50);
                }
            }

            position++;
            bookReadPosition.TryUpdate(bookId, position, position - 1); // �����ֵ��е�λ��
        }
    }
    static ConcurrentDictionary<Guid, bool> bookIsReading = new ConcurrentDictionary<Guid, bool>();
    public void bookGoNext(Guid bookId)
    {
        bookIsReading[bookId] = false;
    }
    public async Task StopReadTask()
    {
        await Clients.All.SendAsync("onStopReadTask");
    }
}
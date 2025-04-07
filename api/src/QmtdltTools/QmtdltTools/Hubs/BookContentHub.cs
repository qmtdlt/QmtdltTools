using System.Collections.Concurrent;
using System.Formats.Tar;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using QmtdltTools.Domain.Models;
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

    public async Task InitCache(Guid bookId)
    {
        var connectionId = Context.ConnectionId;            // connectionId

        var book = await _epubManageService.GetBookById(bookId);            // ��ѯ���ݿ⣬��ȡ book ����
        if (book == null)
        {
            await Clients.All.SendAsync("onShowErrMsg", "δ�ҵ�Id��Ӧ�ĵ�����");
            return;
        }
        if (File.Exists(book.BookPath))                                      // ���������ļ��Ƿ����
        {
            var ebook = EpubHelper.GetEbook(book.BookPath, out string message);  // �������ϵĵ�������ص��ڴ���
            if (ebook == null)
            {
                await Clients.All.SendAsync("onShowErrMsg", message);           // ǰ����ʾ������Ϣ
            }
            else
            {
                int count = ebook.ReadingOrder.Count;                                       // ��ȡ��������½���
                bool success = bookReadingCache.TryGetValue(bookId, out var bookInfo);
                if (!success)
                {
                    bookInfo = new BookReaderModel { ebook = ebook, position = 0 };
                    bookReadingCache.AddOrUpdate(bookId, bookInfo, (bookId, old) => { return bookInfo; });
                }
                await Clients.All.SendAsync("onSetBookPosition", bookInfo.position); // ֪ͨǰ�˳�ʼ������
            }
        }
        else
        {
            await Clients.All.SendAsync("onShowErrMsg", "epub file doesn't exist"); // ǰ����ʾ������Ϣ
        }
    }

    public async Task Read(Guid bookId,int position)
    {
        var connectionId = Context.ConnectionId;            // connectionId
        bookReadingCache[bookId].position = position;       // ���»����е�λ��

        if (bookReadingCache[bookId].position < bookReadingCache[bookId].ebook.ReadingOrder.Count && connectionStatusCache[connectionId])
        {
            var item = bookReadingCache[bookId].ebook.ReadingOrder[bookReadingCache[bookId].position];

            if (!string.IsNullOrEmpty(item.Content))
            {
                // ʹ��������ʽ���� HTML ��ǩ���õ����ı�
                string plainText = Regex.Replace(item.Content, "<[^>]+>", " ");
                // �ɽ�һ�����ı�����������ȥ������ո�
                plainText = Regex.Replace(plainText, "\\s+", " ").Trim();

                if(plainText.IsNullOrEmpty())
                {
                    bookReadingCache[bookId].position += 1;
                    await Clients.All.SendAsync("onSetBookPosition", bookReadingCache[bookId].position); // ֪ͨǰ�˳�ʼ������
                    return;
                }

                var buffer = EpubHelper.GetSpeakStream(plainText);

                try
                {
                    await Clients.All.SendAsync("UIReadInfo", new UIReadInfo
                    {
                        buffer = buffer,
                        text = plainText,
                        position = bookReadingCache[bookId].position
                    });                   // send audio buffer, this will be base64 string on client side
                }
                catch (Exception ex)
                {

                    throw;
                }
            }
        }
    }

    static ConcurrentDictionary<Guid, BookReaderModel> bookReadingCache = new ConcurrentDictionary<Guid, BookReaderModel>();
    static ConcurrentDictionary<string,bool> connectionStatusCache = new ConcurrentDictionary<string,bool>();
    
    public async Task StopReadTask()
    {
        await Clients.All.SendAsync("onStopReadTask");
    }
}
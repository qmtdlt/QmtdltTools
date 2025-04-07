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
        connectionStatusCache.AddOrUpdate(connectionId, true, (connectionId, old) => { return true; }); // 添加连接状态到缓存
        return base.OnConnectedAsync();
    }
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine("on disconnected");
        var connectionId = Context.ConnectionId;
        connectionStatusCache.AddOrUpdate(connectionId, false, (connectionId, old) => { return false; }); // 更新连接状态到缓存
        return base.OnDisconnectedAsync(exception);
    }

    public async Task InitCache(Guid bookId)
    {
        var connectionId = Context.ConnectionId;            // connectionId

        var book = await _epubManageService.GetBookById(bookId);            // 查询数据库，获取 book 对象
        if (book == null)
        {
            await Clients.All.SendAsync("onShowErrMsg", "未找到Id对应的电子书");
            return;
        }
        if (File.Exists(book.BookPath))                                      // 检查电子书文件是否存在
        {
            var ebook = EpubHelper.GetEbook(book.BookPath, out string message);  // 将磁盘上的电子书加载到内存中
            if (ebook == null)
            {
                await Clients.All.SendAsync("onShowErrMsg", message);           // 前端显示错误信息
            }
            else
            {
                int count = ebook.ReadingOrder.Count;                                       // 获取电子书的章节数
                bool success = bookReadingCache.TryGetValue(bookId, out var bookInfo);
                if (!success)
                {
                    bookInfo = new BookReaderModel { ebook = ebook, position = 0 };
                    bookReadingCache.AddOrUpdate(bookId, bookInfo, (bookId, old) => { return bookInfo; });
                }
                await Clients.All.SendAsync("onSetBookPosition", bookInfo.position); // 通知前端初始化缓存
            }
        }
        else
        {
            await Clients.All.SendAsync("onShowErrMsg", "epub file doesn't exist"); // 前端显示错误信息
        }
    }

    public async Task Read(Guid bookId,int position)
    {
        var connectionId = Context.ConnectionId;            // connectionId
        bookReadingCache[bookId].position = position;       // 更新缓存中的位置

        if (bookReadingCache[bookId].position < bookReadingCache[bookId].ebook.ReadingOrder.Count && connectionStatusCache[connectionId])
        {
            var item = bookReadingCache[bookId].ebook.ReadingOrder[bookReadingCache[bookId].position];

            if (!string.IsNullOrEmpty(item.Content))
            {
                // 使用正则表达式剥离 HTML 标签，得到纯文本
                string plainText = Regex.Replace(item.Content, "<[^>]+>", " ");
                // 可进一步对文本进行整理，如去除多余空格
                plainText = Regex.Replace(plainText, "\\s+", " ").Trim();

                if(plainText.IsNullOrEmpty())
                {
                    bookReadingCache[bookId].position += 1;
                    await Clients.All.SendAsync("onSetBookPosition", bookReadingCache[bookId].position); // 通知前端初始化缓存
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
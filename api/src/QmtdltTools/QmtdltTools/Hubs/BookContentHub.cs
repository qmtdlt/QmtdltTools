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

    static ConcurrentDictionary<Guid,int> bookReadPosition = new ConcurrentDictionary<Guid,int>();
    static ConcurrentDictionary<string,bool> connectionStatusCache = new ConcurrentDictionary<string,bool>();
    public async Task StartReadTask(Guid bookId)
    {
        var connectionId = Context.ConnectionId;            // connectionId

        var book = await _epubManageService.GetBookById(bookId);            // 查询数据库，获取 book 对象
        if(book == null)
        {
            await Clients.All.SendAsync("onShowErrMsg", "未找到Id对应的电子书");
            return;
        }
        if(File.Exists(book.BookPath))                                      // 检查电子书文件是否存在
        {
            var ebook = EpubHelper.GetEbook(book.BookPath,out string message);  // 将磁盘上的电子书加载到内存中
            if (ebook == null)
            {
                await Clients.All.SendAsync("onShowErrMsg", message);           // 前端显示错误信息
            }
            else
            {
                _ = StartReadingTask(connectionId, ebook, bookId);        // 启动阅读任务
            }
        }
        else
        {
            await Clients.All.SendAsync("onShowErrMsg", "电子书不存在");
        }
    }

    async Task StartReadingTask(string connectionId, EpubBook ebook,Guid bookId)
    {
        int count = ebook.ReadingOrder.Count;                                       // 获取电子书的章节数
        bool success = bookReadPosition.TryGetValue(bookId, out int position);
        if (!success)
        {
            position = 0;
            // 更新 bookReadPosition 字典
            bookReadPosition.TryAdd(bookId, position);
        }
        while (position < count && connectionStatusCache[connectionId])
        {
            var item = ebook.ReadingOrder[position];

            if (!string.IsNullOrEmpty(item.Content))
            {
                // 使用正则表达式剥离 HTML 标签，得到纯文本
                string plainText = Regex.Replace(item.Content, "<[^>]+>", " ");
                // 可进一步对文本进行整理，如去除多余空格
                plainText = Regex.Replace(plainText, "\\s+", " ").Trim();

                var buffer = EpubHelper.GetSpeakStream(plainText);

                bookIsReading.AddOrUpdate(bookId, true, (bookId, old) => { return true; });

                await Clients.All.SendAsync("onPlayVoiceBuffer", buffer);                   // send audio buffer, this will be base64 string on client side
                await Clients.All.SendAsync("onShowReadingText", plainText);                // send text
                                                                                            // 通知前端 dowork
                while (bookIsReading[bookId])
                {
                    await Task.Delay(50);
                }
            }

            position++;
            bookReadPosition.TryUpdate(bookId, position, position - 1); // 更新字典中的位置
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
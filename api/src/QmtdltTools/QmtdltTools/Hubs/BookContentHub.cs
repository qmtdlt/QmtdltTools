using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Autofac.Core.Resolving.Middleware;
using Microsoft.AspNetCore.SignalR;
using QmtdltTools.Domain.Models;
using QmtdltTools.Service.Services;
using QmtdltTools.Service.Utils;
using Volo.Abp.AspNetCore.SignalR;

namespace QmtdltTools.Hubs;

//[Authorize]
public class BookContentHub:AbpHub
{
    // static dict for all users
    // 
    static ConcurrentDictionary<Guid, BookReaderModel> bookReadingCache = new ConcurrentDictionary<Guid, BookReaderModel>();
    // connction is alive
    static ConcurrentDictionary<string, bool> connectionStatusCache = new ConcurrentDictionary<string, bool>();
    // DI
    private readonly EpubManageService _epubManageService;
    public BookContentHub(EpubManageService epubManageService)
    {
        _epubManageService = epubManageService;
    }

    public override Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        connectionStatusCache.AddOrUpdate(connectionId, true, (connectionId, old) => { return true; });         // set connection alive
        return base.OnConnectedAsync();
    }
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        connectionStatusCache.AddOrUpdate(connectionId, false, (connectionId, old) => { return false; });       // set connection dead

        // clear cache ?? 
        // 持久化 redis bookid position 进度

        return base.OnDisconnectedAsync(exception);
    }

    public async Task InitCache(Guid bookId)
    {
        var connectionId = Context.ConnectionId;                            // connectionId

        var book = await _epubManageService.GetBookById(bookId);            // read book info from database
        if (book == null)
        {
            await Clients.All.SendAsync("onShowErrMsg", "book not found in database");         // 
            return;
        }
        
        if (File.Exists(book.BookPath))                                             // if file exists
        {
            var ebook = EpubHelper.GetEbook(book.BookPath, out string message);     // get ebook
            if (ebook == null)
            {
                await Clients.All.SendAsync("onShowErrMsg", message);               // show err
            }
            else
            {
                List<MyPragraph> plist = EpubHelper.PrepareAllPragraphs(ebook);     // analyse book and get all pragraphs
                // read book reading progress from redis or database
                                                      
                bool success = bookReadingCache.TryGetValue(bookId, out var bookInfo);

                if (!success)
                {
                    bookInfo = new BookReaderModel { plist = plist, position = new ReadPosition {
                        PragraphIndex = 0, SentenceIndex = 0 
                    } };
                    bookReadingCache.AddOrUpdate(bookId, bookInfo, (bookId, old) => { return bookInfo; });
                }

                try
                {
                    bookReadingCache[bookId].position = RedisHelper.Get<ReadPosition>(bookId.ToString());
                }
                catch (Exception)
                { }

                if (null == bookReadingCache[bookId].position) 
                    bookReadingCache[bookId].position = new ReadPosition();

                await Clients.All.SendAsync("onSetBookPosition", bookInfo.position); // 
            }
        }
        else
        {
            await Clients.All.SendAsync("onShowErrMsg", "epub file doesn't exist"); // 
        }
    }
   
    public async Task Read(Guid bookId)
    {
        var connectionId = Context.ConnectionId;            // connectionId

        if (bookReadingCache[bookId].PositionInbook() && connectionStatusCache[connectionId])
        {
            bool success = bookReadingCache[bookId].readQueue.TryDequeue(out UIReadInfo uiReadInfo);
            if (!success)
            {
                success = CurReadInfoEnQueue(bookId);            // get queue data fail,make data(only for first time)
                bookReadingCache[bookId].readQueue.TryDequeue(out uiReadInfo);
            }

            _ = Clients.All.SendAsync("UIReadInfo", uiReadInfo);            // call client speak
            RedisHelper.Set(bookId.ToString(), bookReadingCache[bookId].position);

            bookReadingCache[bookId].PositionNext();                // go next
            success = CurReadInfoEnQueue(bookId);                   // en queue
        }
    }
    public async Task StopReadTask()
    {
        await Clients.All.SendAsync("onStopReadTask");
    }

    bool CurReadInfoEnQueue(Guid bookId)
    {
        bool success = GetCurUIReadInfo(bookId, out UIReadInfo uiReadInfo);
        if (success)
        {
            bookReadingCache[bookId].readQueue.Enqueue(uiReadInfo);
            return true;
        }
        return false;
    }
    bool GetCurUIReadInfo(Guid bookId,out UIReadInfo uiReadInfo)
    {
        uiReadInfo = new UIReadInfo();
        
        uiReadInfo = bookReadingCache[bookId].GetCurrentPosInfo();              // cur position info
        if(null == uiReadInfo) return false;

        if (!string.IsNullOrEmpty(uiReadInfo.speaking_text))
        {
            if(uiReadInfo.speaking_text.IsNullOrEmpty()) return false;
            uiReadInfo.speaking_buffer = EpubHelper.GetSpeakStream(uiReadInfo.speaking_text);       // make speak buffer
            return true;
        }
        return false;
    }
}
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using QmtdltTools.Domain.Models;
using QmtdltTools.Service.Services;
using QmtdltTools.Service.Utils;
using Volo.Abp.AspNetCore.SignalR;

namespace QmtdltTools.Hubs;

//[Authorize]
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
        connectionStatusCache.AddOrUpdate(connectionId, true, (connectionId, old) => { return true; });
        return base.OnConnectedAsync();
    }
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine("on disconnected");
        var connectionId = Context.ConnectionId;
        connectionStatusCache.AddOrUpdate(connectionId, false, (connectionId, old) => { return false; });
        return base.OnDisconnectedAsync(exception);
    }

    public async Task InitCache(Guid bookId)
    {
        var connectionId = Context.ConnectionId;            // connectionId

        var book = await _epubManageService.GetBookById(bookId);            // 
        if (book == null)
        {
            await Clients.All.SendAsync("onShowErrMsg", "book not found");         //
            return;
        }
        if (File.Exists(book.BookPath))                                      // 
        {
            var ebook = EpubHelper.GetEbook(book.BookPath, out string message);  // get ebook
            if (ebook == null)
            {
                await Clients.All.SendAsync("onShowErrMsg", message);           // show err
            }
            else
            {
                int count = ebook.ReadingOrder.Count;                                       // 
                bool success = bookReadingCache.TryGetValue(bookId, out var bookInfo);
                if (!success)
                {
                    bookInfo = new BookReaderModel { ebook = ebook, position = 0 };
                    bookReadingCache.AddOrUpdate(bookId, bookInfo, (bookId, old) => { return bookInfo; });
                }
                await Clients.All.SendAsync("onSetBookPosition", bookInfo.position); // 
            }
        }
        else
        {
            await Clients.All.SendAsync("onShowErrMsg", "epub file doesn't exist"); // 
        }
    }

    public async Task Read(Guid bookId,int position)
    {
        var connectionId = Context.ConnectionId;            // connectionId
        bookReadingCache[bookId].position = position;       // pos

        if (bookReadingCache[bookId].position < bookReadingCache[bookId].ebook.ReadingOrder.Count && connectionStatusCache[connectionId])
        {
            bool success = bookReadingCache[bookId].readQueue.TryDequeue(out UIReadInfo uiReadInfo);
            if (success)
            {
                _ = Clients.All.SendAsync("UIReadInfo", uiReadInfo);
            }
            else
            {
                success = MakeQueueData(bookId);
                
                if(!success)
                {
                    bookReadingCache[bookId].position += 1;
                    await Clients.All.SendAsync("onSetBookPosition", bookReadingCache[bookId].position); 
                    return;
                }
                else
                {
                    success = bookReadingCache[bookId].readQueue.TryDequeue(out UIReadInfo uiReadInfo2);
                    if(success)
                        _ = Clients.All.SendAsync("UIReadInfo", uiReadInfo2);
                }
            }

            bookReadingCache[bookId].position += 1; // pos
            MakeQueueData(bookId);
        }
    }
    public async Task StopReadTask()
    {
        await Clients.All.SendAsync("onStopReadTask");
    }

    bool MakeQueueData(Guid bookId)
    {
        bool success = GetCurContent(bookId, out byte[] buffer,out string plainText);
        if (success)
        {
            bookReadingCache[bookId].readQueue.Enqueue(new UIReadInfo
            {
                buffer = buffer,
                text = plainText,
                position = bookReadingCache[bookId].position
            });
        }
        return success;
    }
    bool GetCurContent(Guid bookId,out byte[] buffer,out string plainText)
    {
        buffer = null;
        plainText = "";
        Console.WriteLine("prepare position: " + bookReadingCache[bookId].position);
        var item = bookReadingCache[bookId].ebook.ReadingOrder[bookReadingCache[bookId].position];
        
        if (!string.IsNullOrEmpty(item.Content))
        {
            // get plain text
            plainText = Regex.Replace(item.Content, "<[^>]+>", " ");
            // remove spaces
            plainText = Regex.Replace(plainText, "\\s+", " ").Trim();

            if(plainText.IsNullOrEmpty()) return false;
            buffer = EpubHelper.GetSpeakStream(plainText);
            return true;
        }
        if(bookReadingCache[bookId].position + 1 < bookReadingCache[bookId].ebook.ReadingOrder.Count)
        {
            return false;
        }
        return false;
    }
    
    static ConcurrentDictionary<Guid, BookReaderModel> bookReadingCache = new ConcurrentDictionary<Guid, BookReaderModel>();
    static ConcurrentDictionary<string,bool> connectionStatusCache = new ConcurrentDictionary<string,bool>();
    
}
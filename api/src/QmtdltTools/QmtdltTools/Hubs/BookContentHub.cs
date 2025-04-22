using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Autofac.Core.Resolving.Middleware;
using Castle.Components.DictionaryAdapter.Xml;
using Microsoft.AspNetCore.SignalR;
using QmtdltTools.Domain.Dtos;
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
    private readonly ListenWriteService _listenWriteService;
    private readonly TranslationService _translationService;
    private Stopwatch _sw;
    public BookContentHub(EpubManageService epubManageService, ListenWriteService listenWriteService, TranslationService translationService)
    {
        _epubManageService = epubManageService;
        _listenWriteService = listenWriteService;
        _translationService = translationService;
        _sw = new Stopwatch();
    }

    public override Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        connectionStatusCache.AddOrUpdate(connectionId, true, (connectionId, old) => { return true; });         // set connection alive

        var clientProxy = Clients.Caller;
        Task.Run(async () =>
        {
            _sw.Start();
            while (connectionStatusCache[connectionId])
            {
                double totalSeconds = _sw.Elapsed.TotalSeconds;

                // totalSeconds format as x hour y min z sec

                // 转换为小时、分钟和秒
                int hours = (int)(totalSeconds / 3600);
                int minutes = (int)((totalSeconds % 3600) / 60);
                int seconds = (int)(totalSeconds % 60);

                // 格式化为字符串
                string formattedTime = $"{hours}hour {minutes}minitues {seconds}seconds";

                await clientProxy.SendAsync("onUpdateWatch",formattedTime);

                await Task.Delay(1000);
            }
        });

        return base.OnConnectedAsync();
    }
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        connectionStatusCache.AddOrUpdate(connectionId, false, (connectionId, old) => { return false; });       // set connection dead

        _sw.Stop();

        // 从 connectionStatusCache 中移除 connectionId 对应数据
        connectionStatusCache.TryRemove(connectionId, out _);

        return base.OnDisconnectedAsync(exception);
    }

    public async Task InitCache(Guid bookId)
    {
        var connectionId = Context.ConnectionId;                            // connectionId

        var book = await _epubManageService.GetBookById(bookId);            // read book info from database
        if (book == null)
        {
            await Clients.Caller.SendAsync("onShowErrMsg", "book not found in database");         // 
            return;
        }
        
        if (File.Exists(book.BookPath))                                             // if file exists
        {
            var ebook = EpubHelper.GetEbook(book.BookPath, out string message);     // get ebook
            if (ebook == null)
            {
                await Clients.Caller.SendAsync("onShowErrMsg", message);               // show err
            }
            else
            {
                List<MyPragraph> plist = EpubHelper.PrepareAllPragraphs(ebook);     // analyse book and get all pragraphs
                Console.WriteLine($"plist count:{plist.Count}");
                // make dictionary cache
                bool success = bookReadingCache.TryGetValue(bookId, out var bookInfo);                              // try get from dictionary

                if (!success)
                {
                    bookInfo = new BookReaderModel
                    {
                        plist = plist,
                        position = new ReadPosition { PragraphIndex = 0, SentenceIndex = 0 },
                    };
                    bookReadingCache.AddOrUpdate(bookId, bookInfo, (bookId, old) => { return bookInfo; });          // first add bookInfo to cache
                }

                // read book reading progress from redis or database
                Console.WriteLine("read book reading progress from redis or database");
                try
                {
                    bookReadingCache[bookId].position = RedisHelper.Get<ReadPosition>(bookId.ToString());
                }
                catch (Exception)
                { }

                if (null == bookReadingCache[bookId].position)
                    bookReadingCache[bookId].position = new ReadPosition();

                Console.WriteLine("start CurReadInfoEnQueue");
                try
                {
                    success = CurReadInfoEnQueue(bookId, out UIReadInfo uiReadInfo);
                    Console.WriteLine("onSetBookPosition");
                    await Clients.Caller.SendAsync("onSetBookPosition", uiReadInfo); //    
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                Console.WriteLine("init cache end");
            }
        }
        else
        {
            await Clients.Caller.SendAsync("onShowErrMsg", "epub file doesn't exist"); // 
        }
    }
    public async Task Trans(Guid bookId,string word)
    {
        var findRes = await _translationService.Find(bookId, bookReadingCache[bookId].position.PragraphIndex, bookReadingCache[bookId].position.SentenceIndex, word);
        if(findRes != null)
        {
            await Clients.Caller.SendAsync("onShowTrans", new TranslateDto
            {
                Explanation = findRes.AIExplanation,
                Translation = findRes.AITranslation,
                VoiceBuffer = findRes.Pronunciation
            });
            return;
        }
        var res = await GeminiRestHelper.GetTranslateResult(word);
        if (res != null)
        {
            await Clients.Caller.SendAsync("onShowTrans", res);
            await _translationService.AddRecord(new Domain.Entitys.VocabularyRecord
            {
                BookId = bookId,
                WordText = word,
                Pronunciation = res.VoiceBuffer,
                AIExplanation = res.Explanation,
                AITranslation = res.Translation,
            });
        }
        else
        {
            await Clients.Caller.SendAsync("onShowErrMsg", "翻译失败");
        }
    }
    public void Read(Guid bookId)
    {
        var connectionId = Context.ConnectionId;            // connectionId

        if (bookReadingCache[bookId].PositionInbook() && connectionStatusCache[connectionId])
        {
            bool success = bookReadingCache[bookId].readQueue.TryDequeue(out UIReadInfo uiReadInfo);
            if (!success)
            {
                success = CurReadInfoEnQueue(bookId,out UIReadInfo enQueueInfo1);            // get queue data fail,make data(only for first time)
                bookReadingCache[bookId].readQueue.TryDequeue(out uiReadInfo);
            }

            _ = Clients.Caller.SendAsync("UIReadInfo", uiReadInfo);            // call client speak
            RedisHelper.Set(bookId.ToString(), bookReadingCache[bookId].position);

            bookReadingCache[bookId].PositionNext();                // go next
            success = CurReadInfoEnQueue(bookId, out UIReadInfo enQueueInfo2);                   // en queue
        }
    }
    public async Task ResetPosition(Guid bookId ,int offsetPos)
    {
        bookReadingCache[bookId].readQueue.Clear();
        bookReadingCache[bookId].ResetPosition(offsetPos);
        RedisHelper.Set(bookId.ToString(), bookReadingCache[bookId].position);
        await InitCache(bookId);
    }

    bool CurReadInfoEnQueue(Guid bookId, out UIReadInfo uiReadInfo)
    {
        bool success = GetCurUIReadInfo(bookId, out uiReadInfo);
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
            uiReadInfo.speaking_buffer = TTSHelperRest.GetSpeakStreamRest(uiReadInfo.speaking_text, uiReadInfo.voice_name);       // make speak buffer
            return true;
        }
        return false;
    }
}
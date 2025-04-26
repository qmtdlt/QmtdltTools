using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using QmtdltTools.Domain.Dtos;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.Domain.Models;
using QmtdltTools.Service.Services;
using QmtdltTools.Service.Utils;
using Volo.Abp.AspNetCore.SignalR;

namespace QmtdltTools.Hubs;

[Authorize]
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
    public BookContentHub(EpubManageService epubManageService, ListenWriteService listenWriteService, 
        TranslationService translationService)
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

                // ת��ΪСʱ�����Ӻ���
                int hours = (int)(totalSeconds / 3600);
                int minutes = (int)((totalSeconds % 3600) / 60);
                int seconds = (int)(totalSeconds % 60);

                // ��ʽ��Ϊ�ַ���
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

        // �� connectionStatusCache ���Ƴ� connectionId ��Ӧ����
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
                bookReadingCache[bookId].readQueue.Clear();
                bookReadingCache[bookId].readQueue = new ConcurrentQueue<UIReadInfo>();
                try
                {
                    bookReadingCache[bookId].position = RedisHelper.Get<ReadPosition>(bookId.ToString());
                }
                catch (Exception)
                { }

                if (null == bookReadingCache[bookId].position) bookReadingCache[bookId].position = new ReadPosition();

                success = CurReadInfoEnQueue(bookId, out UIReadInfo uiReadInfo);
                await Clients.Caller.SendAsync("onSetBookPosition", uiReadInfo);
            }
        }
        else
        {
            await Clients.Caller.SendAsync("onShowErrMsg", "epub file doesn't exist"); // 
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
                success = CurReadInfoEnQueue(bookId, out UIReadInfo enQueueInfo1);            // get queue data fail,make data(only for first time)
                bookReadingCache[bookId].readQueue.TryDequeue(out uiReadInfo);
            }

            _ = Clients.Caller.SendAsync("UIReadInfo", uiReadInfo);            // call client speak
            RedisHelper.Set(bookId.ToString(), bookReadingCache[bookId].position);

            bookReadingCache[bookId].PositionNext();                // go next
            success = CurReadInfoEnQueue(bookId, out UIReadInfo enQueueInfo2);                   // en queue
        }
    }
    public async Task Trans(Guid bookId,string word)
    {
        var uid = Context.GetUserId();
        VocabularyRecord? findRes = await _translationService.Find(bookId,
            bookReadingCache[bookId].position.PragraphIndex, 
            bookReadingCache[bookId].position.SentenceIndex, 
            bookReadingCache[bookId].plist[bookReadingCache[bookId].position.PragraphIndex].Sentences[bookReadingCache[bookId].position.SentenceIndex], // get word from database
            word,uid);
        if(findRes != null)
        {
            await Clients.Caller.SendAsync("onShowTrans", new TranslateDto
            {
                Explanation = findRes.AIExplanation,
                Translation = findRes.AITranslation,
                VoiceBuffer = findRes.Pronunciation,
                WordVoiceBuffer = findRes.WordPronunciation
            });
            return;
        }
        await Clients.Caller.SendAsync("onShowErrMsg", "����ʧ��");
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
        else
        {
            RedisHelper.Set(bookId.ToString(), bookReadingCache[bookId].position);
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
            try
            {
                uiReadInfo.speaking_buffer = MsTTSHelperRest.GetSpeakStreamRest(uiReadInfo.speaking_text, uiReadInfo.voice_name);       // make speak buffer
                return true;
            }
            catch (Exception ex)
            {
                _ = Clients.Caller.SendAsync("onShowErrMsg", "tts call err:" + ex.Message); // 
                return false;
            }
        }
        return false;
    }
}
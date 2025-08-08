using Autofac.Core;
using MovieCollection.OpenSubtitles;
using MovieCollection.OpenSubtitles.Models;
using QmtdltTools.Avaloina.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Avaloina
{
    public class OpenSubtitlesAPIService:ITransientDependency
    {
        OpenSubtitlesService? _service = null;

        public OpenSubtitlesAPIService()
        {
            _service = new OpenSubtitlesService(new HttpClient(), new OpenSubtitlesOptions
            {
                ApiKey = AppSettingHelper.OpenSubtitleApiKey,
                ProductInformation = new ProductHeaderValue("youngforyou", "1.0"),
            });
        }

        // public async Task<string> DownloadSubtitle(int fileId,string targetSaveDir)
        // {
        //     var download = new NewDownload
        //     {
        //         FileId = fileId,
        //     };
        //
        //     var result = await _service.GetSubtitleForDownloadAsync(download, "");
        //     var path = Path.Combine(targetSaveDir, result.FileName);
        //
        //     try
        //     {
        //         var webClient = new WebClient();
        //         await webClient.DownloadFileTaskAsync(result.Link, path);
        //         return path;
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"Download file error: {ex.Message}");
        //         return "";
        //     }
        // }
        public async Task<string> DownloadSubtitle(int fileId, string targetSaveDir)
        {
            var download = new NewDownload { FileId = fileId };

            // 取得下载信息（含文件名与直链）
            Download? result = await _service.GetSubtitleForDownloadAsync(download, "");
            if (result == null || string.IsNullOrWhiteSpace(result.FileName))
                return "";

            // 确保目标目录存在（支持传入绝对路径）
            if (string.IsNullOrWhiteSpace(targetSaveDir))
                return "";

            Directory.CreateDirectory(targetSaveDir); // ✅ 若不存在则创建

            // 清理一下文件名（跨平台安全）
            var fileName = string.Join("_", (result.FileName ?? "subtitle.srt")
                .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

            var path = Path.Combine(targetSaveDir, fileName);

            try
            {
                using var http = new HttpClient();
                using var resp = await http.GetAsync(result.Link, HttpCompletionOption.ResponseHeadersRead);
                resp.EnsureSuccessStatusCode();

                await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                await using var stream = await resp.Content.ReadAsStreamAsync();
                await stream.CopyToAsync(fs);

                return path; // ✅ 返回实际保存的绝对路径
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Download file error: {ex.Message}");
                return "";
            }
        }

        public async Task<PagedResult<AttributeResult<Subtitle>>> SearchSubtitles(string moviePath,int page = 0)
        {
            var search = new NewSubtitleSearch
            {
                Query = Path.GetFileName(moviePath),
                MovieHash = OpenSubtitlesHasher.GetFileHash(moviePath),
                Languages = new List<string> { "en" }
            };
            PagedResult<AttributeResult<Subtitle>> result = await _service.SearchSubtitlesAsync(search);
            return result;
        }
    }
}

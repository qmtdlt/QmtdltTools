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

        public async Task<string> DownloadSubtitle(int fileId,string targetSaveDir)
        {
            var download = new NewDownload
            {
                FileId = fileId,
            };

            var result = await _service.GetSubtitleForDownloadAsync(download, "");
            var path = Path.Combine(targetSaveDir, result.FileName);

            try
            {
                var webClient = new WebClient();
                await webClient.DownloadFileTaskAsync(result.Link, path);
                return path;
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

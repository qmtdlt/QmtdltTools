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
        public OpenSubtitlesAPIService()
        {
            
        }
        static string apiKey = "";
        static string openSubTitleUName = AppSettingHelper.ApiServer;
        static string openSubTitleUPwd = AppSettingHelper.ApiServer;
        private static string _token = "";
        static HttpClient httpClient = new HttpClient();
        static OpenSubtitlesOptions options = new OpenSubtitlesOptions
        {
            ApiKey = apiKey,
            ProductInformation = new ProductHeaderValue("youngforyou", "1.0"),
        };

        static OpenSubtitlesService _service = new OpenSubtitlesService(httpClient, options);


        public async Task<string> DownloadSubtitle(int fileId,string targetSaveDir)
        {
            var download = new NewDownload
            {
                FileId = fileId,
            };

            var result = await _service.GetSubtitleForDownloadAsync(download, _token);

            Console.WriteLine($"FileName: {result.FileName}");
            Console.WriteLine($"Requests: {result.Requests}");
            Console.WriteLine($"Remaining: {result.Remaining}");
            Console.WriteLine($"Message: {result.Message}");
            Console.WriteLine($"Link: {result.Link}");

            var path = Path.Combine(targetSaveDir, result.FileName);

            try
            {
                Console.WriteLine($"Downloading to: {path}");

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
        private static async Task Login()
        {
            if (!string.IsNullOrEmpty(_token))
            {
                Console.WriteLine("Already logged in.");
                return;
            }

            var login = new NewLogin
            {
                Username = openSubTitleUName,
                Password = openSubTitleUPwd,
            };

            var result = await _service.LoginAsync(login);

            Console.WriteLine($"Status: {result.Status}");

            if (result.Status == 200)
            {
                // Login was successful, save the token.
                _token = result.Token;

                Console.WriteLine($"Token: {result.Token}");
                Console.WriteLine($"AllowedDownloads: {result.User.AllowedDownloads}");
                Console.WriteLine($"Level: {result.User.Level}");
                Console.WriteLine($"UserId: {result.User.UserId}");
                Console.WriteLine($"ExtInstalled: {result.User.ExtInstalled}");
                Console.WriteLine($"Vip: {result.User.Vip}");
            }
            else
            {
                // Login failed, show the error message.
                Console.WriteLine($"Message: {result.Message}");
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

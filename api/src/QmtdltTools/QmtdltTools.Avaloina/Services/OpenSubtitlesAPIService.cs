using MovieCollection.OpenSubtitles;
using MovieCollection.OpenSubtitles.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Avaloina
{
    public class OpenSubtitlesAPIService:ITransientDependency
    {
        //private static readonly HttpClient httpClient = new HttpClient();
        public OpenSubtitlesAPIService()
        {
            
        }
        // 
        public async Task<PagedResult<AttributeResult<Subtitle>>> SearchSubtitles(string moviePath,int page = 0)
        {
            HttpClient httpClient = new HttpClient();
            var options = new OpenSubtitlesOptions
            {
                ApiKey = "7qctHuKctHoLONmiWrwp5u8PJnDxE9Hf",
                ProductInformation = new ProductHeaderValue("youngforyou", "1.0"),
            };

            var service = new OpenSubtitlesService(httpClient, options);

            var search = new NewSubtitleSearch
            {
                Query = Path.GetFileName(moviePath),
                MovieHash = OpenSubtitlesHasher.GetFileHash(moviePath),
                Languages = new List<string> { "en" }
            };

            PagedResult<AttributeResult<Subtitle>> result = await service.SearchSubtitlesAsync(search);
            return result;
        }
    }
}

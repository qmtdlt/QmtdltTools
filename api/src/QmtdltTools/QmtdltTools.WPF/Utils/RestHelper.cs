using CefSharp;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.WPF.Dto;
using RestSharp;
using ScottPlot.Palettes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core.Tokenizer;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QmtdltTools.WPF.Utils
{
    public class RestHelper
    {
        static string token = "";
        public static async Task<bool> login(string acc, string pwd)
        {
            try
            {
                var postdata = new
                {
                    username = acc,
                    password = pwd
                };
                var endpoint = $"{AppSettingHelper.ApiServer}/api/Auth/Login/login";
                var client = new RestClient();
                var request = new RestRequest(endpoint, Method.Post);

                request.AddHeader("Accept", "text/plain");
                request.AddHeader("Content-Type", "application/json-patch+json");
                request.AddJsonBody(postdata);

                var response = await client.ExecuteAsync(request);
                var dto = JsonSerializer.Deserialize<LoginResult>(response.Content);

                token = dto.token;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        /* js 代码，改C#版本
        let res = await request.get<VocabularyRecord>(
            '/api/Vocabulary/Trans',
            { params: { word: tmpTransText }
        }
        );*/

        public static async Task<VocabularyRecordDto?> Trans(string tmpTransText)
        {
            var endpoint = $"{AppSettingHelper.ApiServer}/api/Vocabulary/Trans?word={tmpTransText}";

            var client = new RestClient();
            var request = new RestRequest(endpoint, Method.Get);

            request.AddHeader("Accept", "text/plain");
            request.AddHeader("Content-Type", "application/json-patch+json");
            request.AddHeader("Authorization", $"Bearer {token}");

            var response = await client.ExecuteAsync(request);
            var dto = JsonSerializer.Deserialize<VocabularyRecordDto>(response.Content);
            return dto;
        }
    }
}

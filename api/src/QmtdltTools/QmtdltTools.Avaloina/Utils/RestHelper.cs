using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using QmtdltTools.Avaloina.Dto;
using RestSharp;

namespace QmtdltTools.Avaloina.Utils;

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

    public static async Task<PronunciationAssessmentResultDto?> CheckShadowing(string audioFilePath, string reftext)
    {
        var endpoint = $"{AppSettingHelper.ApiServer}/api/Shadowing/CheckShadowing?reftext={Uri.EscapeDataString(reftext)}";

        var client = new RestClient();
        var request = new RestRequest(endpoint, Method.Post);

        request.AddHeader("Authorization", $"Bearer {token}");
        request.AlwaysMultipartFormData = true;

        // 添加音频文件
        request.AddFile("audioFile", audioFilePath, "audio/wav");

        // 发送请求
        var response = await client.ExecuteAsync(request);

        if (!response.IsSuccessful)
        {
            Console.WriteLine($"Request failed: {response.StatusCode} - {response.Content}");
            return null;
        }

        try
        {
            var result = JsonSerializer.Deserialize<PronunciationAssessmentResultDto>(response.Content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Deserialization error: " + ex.Message);
            return null;
        }
    }
}
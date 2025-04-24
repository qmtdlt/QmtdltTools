using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QmtdltTools.Domain.Data;
using QmtdltTools.Domain.Dtos;
using RestSharp;

namespace QmtdltTools.Service.Utils
{
    public class GeminiRestHelper
    {
        public static string gemini_model = "gemini-2.0-flash-lite";
        public static string apiEndpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{gemini_model}:generateContent?key={ApplicationConst.GEMINI_KEY}";

        public static async Task<TranslateDto> GetTranslateResult(string word)
        {
            // Construct the request body
            var requestBody = new
            {
                contents = new[]
                {
                    new{
                        parts = new []{ 
                            new { text = $@"Please provide an explanation of the word ""{word}"" and its translation into Chinese. Format your response as a JSON object with two fields: ""Explanation"" and ""Translation""." }
                        }
                    }
                }
            };
            var result = await GetResult<TranslateDto>(requestBody);

            if (null != result)
            {
                try
                {
                    result.VoiceBuffer = MsTTSHelperRest.GetSpeakStreamRest(result.Explanation, ApplicationConst.DefaultVoiceName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            return result;
        }

        public static async Task<SentenceEvaluateDto> GetSentenctevaluate(string sentence, string word)
        {
            // Construct the request body
            var requestBody = new
            {
                contents = new[]
                {
                        new{
                            parts = new []{
                                new { text = $@"Check this sentence ""{sentence}"" for {word}, is the usage of {word} correct? If not, give the reason and the corrected sentence. 
                Format your response as a JSON object with three fields: IfUsageCorrect , IncorrectReason and CorrectSentence."
                            }
                        }
                    }
                }
            };
            var result = await GetResult<SentenceEvaluateDto>(requestBody);
            return result;
        }
        public static async Task<T> GetResult<T>(object requestBody)
        {
            //var client = new RestClient();
            var client = new RestClient();
            var request = new RestRequest(apiEndpoint, Method.Post);
            // Set headers
            request.AddHeader("Content-Type", "application/json");

            // Add JSON body to the request
            request.AddJsonBody(requestBody);

            // Execute the request
            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                try
                {
                    // Parse the response into a dynamic object
                    var jsonResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);
                    // Extract the assistant's message content
                    string assistantMessage = jsonResponse.candidates[0].content.parts[0].text.ToString();
                    assistantMessage = assistantMessage.Replace("```json", "").Replace("```", "");
                    // Deserialize the message into the VibeResponse class
                    var result = JsonConvert.DeserializeObject<T>(assistantMessage);

                    return result;
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Error parsing JSON response: {ex.Message}");
                    return default;
                }
            }
            else
            {
                Console.WriteLine($"API request failed: {response.ErrorMessage}");
                return default;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;
using QmtdltTools.Domain.Data;
using QmtdltTools.Domain.Dtos;
using QmtdltTools.Domain.Models;
using RestSharp;

namespace QmtdltTools.Service.Utils
{
    public class RestHelper
    {
        //public static string grok_api_key = Environment.GetEnvironmentVariable("grok_api_key");
        public static string grok_api_key = Environment.GetEnvironmentVariable("GROK_KEY");
        public static string apiEndpoint = "https://api.x.ai/v1/chat/completions";
        public static string grok_model = "grok-3-mini-beta";


        public static async Task<TranslateDto> GetTranslateResult(string word)
        {
            // Initialize RestClient
            var client = new RestClient();
            var request = new RestRequest(apiEndpoint, Method.Post);

            // Set headers
            request.AddHeader("Content-Type", "application/json");
            // Uncomment and add your API key if required
            request.AddHeader("Authorization", $"Bearer {grok_api_key}");

            // Construct the request body
            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = $@"Please provide an explanation of the word ""{word}"" and its translation into Chinese. Format your response as a JSON object with two fields: ""Explanation"" and ""Translation""." }
                },
                model = grok_model,
                stream = false,
                temperature = 0
            };

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
                    string assistantMessage = jsonResponse.choices[0].message.content.ToString();
                    // Deserialize the message into the VibeResponse class
                    var result = JsonConvert.DeserializeObject<TranslateDto>(assistantMessage);
                    if(null != result)
                    {
                        result.VoiceBuffer = EpubHelper.GetSpeakStream(result.Explanation, ApplicationConst.DefaultVoiceName);
                    }
                    return result;
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Error parsing JSON response: {ex.Message}");
                    return null;
                }
            }
            else
            {
                Console.WriteLine($"API request failed: {response.ErrorMessage}");
                return null;
            }
        }
    }
}

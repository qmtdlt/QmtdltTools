using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using QmtdltTools.Domain.Data; // Required for HttpUtility.HtmlEncode

public class TTSHelperRest
{
    // Replace with your actual key and region constants
    public static string speechKey = ApplicationConst.SPEECH_KEY;
    public static string speechRegion = ApplicationConst.SPEECH_REGION;

    /// <summary>
    /// Generate by Grok:
    /// Generates speech audio using the Azure Text-to-Speech REST API.
    /// </summary>
    /// <param name="text">The text to synthesize.</param>
    /// <param name="SpeechSynthesisVoiceName">The name of the voice to use (e.g., "en-US-AvaMultilingualNeural").</param>
    /// <returns>A byte array containing the audio data (MP3 format).</returns>
    /// <exception cref="Exception">Thrown if the REST API call fails.</exception>
    public static byte[] GetSpeakStreamRest(string text, string SpeechSynthesisVoiceName)
    {
        // Using HttpClient in a using block for simplicity in this example.
        // For production scenarios, consider using a single static HttpClient instance
        // or IHttpClientFactory to avoid socket exhaustion.
        using (var client = new HttpClient())
        {
            // Construct the SSML body. SSML is required to specify the voice name via REST API.
            // Escape the text to prevent issues with XML special characters.
            var escapedText = HttpUtility.HtmlEncode(text);

// Extract the language code from the voice name (e.g., "en-US" from "en-US-AvaMultilingualNeural")
// This is a common pattern, but might need adjustment for specific voice names.
// A more robust solution might involve passing the language code separately
// or having a lookup table if voice names don't follow this strict pattern.
            string languageCode = "en-US"; // Default or extract from voice name if possible
// Simple attempt to extract language code (e.g., "en-US" from "en-US-AvaMultilingualNeural")
// This is a basic approach and might need refinement.
            var voiceNameParts = SpeechSynthesisVoiceName.Split('-');
            if (voiceNameParts.Length >= 2)
            {
                languageCode = $"{voiceNameParts[0]}-{voiceNameParts[1]}";
            }


            var ssml = $@"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='http://www.microsoft.com/css/local' xml:lang='{languageCode}'>
    <voice name='{SpeechSynthesisVoiceName}'>
        {escapedText}
    </voice>
</speak>";

            // Set request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", speechKey);
            // Specify the audio format - matching the SDK's Audio16Khz32KBitRateMonoMp3
            client.DefaultRequestHeaders.Add("X-Microsoft-OutputFormat", "audio-16khz-32kbitrate-mono-mp3");
            // Set a User-Agent header (recommended)
            client.DefaultRequestHeaders.Add("User-Agent", "YourAppName"); // Replace with your application name

            // Construct the endpoint URL
            var endpoint = $"https://{speechRegion}.tts.speech.microsoft.com/cognitiveservices/v1";

            // Create the request content with SSML body and content type
            var content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml");

            try
            {
                // Send the POST request synchronously (matching original SDK code pattern)
                // In a real-world application, prefer using async/await with PostAsync
                HttpResponseMessage response = client.PostAsync(endpoint, content).GetAwaiter().GetResult();

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Read the audio data from the response body as a byte array
                    byte[] audioBytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                    return audioBytes;
                }
                else
                {
                    // Read the error message from the response
                    string errorMessage = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    // Log the error (replace with your logging mechanism)
                    // Log.Error($"Speech synthesis REST API failed: {response.StatusCode} - {errorMessage}");
                    Console.Error.WriteLine($"Speech synthesis REST API failed: {response.StatusCode} - {errorMessage}"); // Example logging
                    // Return empty array or throw exception based on desired behavior
                    return new byte[0];
                }
            }
            catch (Exception ex)
            {
                // Log exception (replace with your logging mechanism)
                // Log.Error($"An error occurred during REST API call: {ex.Message}");
                Console.Error.WriteLine($"An error occurred during REST API call: {ex.Message}"); // Example logging
                throw new Exception("Failed to synthesize speech via REST API.", ex);
            }
        }
    }
}

// Note: You need to replace ApplicationConst.SPEECH_KEY and ApplicationConst.SPEECH_REGION
// with your actual constants or configuration values.
//
// You also need to add a reference to System.Web for HttpUtility.HtmlEncode
// if you are not in a web project. In .NET Core/.NET 5+, you might need to add
// the NuGet package Microsoft.AspNetCore.WebUtilities or use System.Net.WebUtility.HtmlEncode.
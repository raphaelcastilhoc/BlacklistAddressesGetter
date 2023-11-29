using BlacklistAddressesGetter.DTOs.OpenAi;
using System.Text;
using System.Text.Json;

namespace BlacklistAddressesGetter.Handlers
{
    public class AIHandler
    {
        private static List<KeyValuePair<string, string>> _contextMessages = new List<KeyValuePair<string, string>>();

        public async static Task<bool> CheckIfAddressIsMaliciousAsync(string address, string[] contextMessages)
        {
            var prompt = $"Based on this context '{string.Join(',', contextMessages)}'. Is it {address} malicious? Answer only true or false";

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
            var openAiChatMessage = new OpenAiChatMessageDto("user", prompt);
            var messages = new OpenAiChatMessageDto[] { openAiChatMessage };
            var payload = new OpenaiRequestPayloadDto("gpt-3.5-turbo-1106", messages, 0);

            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            var jsonContent = JsonSerializer.Serialize(payload, serializeOptions);

            var url = "https://api.openai.com/v1/chat/completions";
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            httpClient.BaseAddress = new Uri(url);

            var openaiResponse = await httpClient.PostAsync(url, httpContent);
            var resContent = await openaiResponse.Content.ReadAsStringAsync();
            var openAiHttpResponseDto = JsonSerializer.Deserialize<OpenAiHttpResponseDto>(resContent, serializeOptions);

            bool.TryParse(openAiHttpResponseDto.Choices.First().Message.Content, out bool addressIsMalicious);

            return addressIsMalicious;
        }
    }
}

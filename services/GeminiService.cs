using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TeleBot.Config;

namespace TeleBot.Services
{
    public class GeminiService
    {
        private readonly string _apiKey;
        private readonly HttpClient _http;

        public GeminiService(AppConfig config)
        {
            _apiKey = config.Gemini.ApiKey;
            _http = new HttpClient();
        }

        public async Task<string> AskAsync(string input)
        {
            if (string.IsNullOrEmpty(_apiKey))
                return "❌ Chưa cấu hình API Key cho Gemini.";

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = input } }
                    }
                }
            };

            string url = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            var jsonBody = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            try
            {
                var response = await _http.PostAsync(url, jsonBody);
                string json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"⚠️ Lỗi Gemini API ({(int)response.StatusCode}): {json}";
                }

                using var doc = JsonDocument.Parse(json);

                // Kiểm tra tồn tại trường cần thiết
                if (doc.RootElement.TryGetProperty("candidates", out var candidates))
                {
                    var text = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    return string.IsNullOrWhiteSpace(text)
                        ? "🤔 Gemini không phản hồi nội dung nào."
                        : text.Trim();
                }

                // Nếu không có candidates mà có promptFeedback
                if (doc.RootElement.TryGetProperty("promptFeedback", out var feedback))
                {
                    return $"⚠️ Gemini từ chối phản hồi: {feedback.ToString()}";
                }

                return "⚠️ Không nhận được phản hồi hợp lệ từ Gemini.";
            }
            catch (HttpRequestException ex)
            {
                return $"⚠️ Lỗi mạng khi kết nối Gemini: {ex.Message}";
            }
            catch (JsonException ex)
            {
                return $"⚠️ Lỗi phân tích JSON: {ex.Message}";
            }
            catch (System.Exception ex)
            {
                return $"⚠️ Lỗi không xác định: {ex.Message}";
            }
        }
    }
}

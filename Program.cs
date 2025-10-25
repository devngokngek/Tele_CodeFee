using System;
using System.Threading.Tasks;
using TeleBot.Config;
using TeleBot.Services;

class Program
{
    static async Task Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8; // hỗ trợ tiếng Việt

        var config = AppConfig.Load("appsettings.json");

        if (string.IsNullOrEmpty(config.Bot.Token))
        {
            Console.WriteLine("❌ Không tìm thấy token trong appsettings.json > Bot.Token");
            return;
        }

        var botService = new TelegramService(config);

        try
        {
            await botService.StartAsync();

            Console.WriteLine("🤖 Bot đang chạy... Nhấn Ctrl+C để dừng.");
            await Task.Delay(-1); // Giữ bot chạy liên tục
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Lỗi khởi động bot: {ex.Message}");
        }
    }
}

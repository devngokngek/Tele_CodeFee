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
            // Chạy KeepAlive server và Bot cùng lúc
            var keepAliveTask = KeepAliveServer.StartAsync();
            var botTask = botService.StartAsync();

            Console.WriteLine("🤖 Bot đang chạy cùng KeepAlive Server... Nhấn Ctrl+C để dừng.");

            await Task.WhenAll(keepAliveTask, botTask); // đợi cả 2 task
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Lỗi khởi động bot hoặc server: {ex.Message}");
        }
    }
}

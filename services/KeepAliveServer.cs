using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TeleBot.Services
{
    public static class KeepAliveServer
    {
        public static async Task StartAsync()
        {
            try
            {
                var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
                var url = $"http://localhost:{port}/";


                var listener = new HttpListener();
                listener.Prefixes.Add(url);
                listener.Start();

                // 🔥 Log ra ngay khi server bắt đầu
                Console.WriteLine($"🌐 KeepAlive server đang lắng nghe tại {url}");

                // 🔄 Task chạy nền
                _ = Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            var ctx = await listener.GetContextAsync();
                            var response = ctx.Response;

                            var message = Encoding.UTF8.GetBytes("✅ Bot is running on Render!");
                            response.ContentType = "text/plain; charset=utf-8";
                            response.ContentLength64 = message.Length;
                            await response.OutputStream.WriteAsync(message, 0, message.Length);
                            response.OutputStream.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ Lỗi KeepAlive: {ex.Message}");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khi khởi động KeepAlive server: {ex.Message}");
            }
        }
    }
}

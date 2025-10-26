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
                // Lấy PORT từ Render (Render luôn đặt biến môi trường PORT)
                var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
                var prefix = $"http://0.0.0.0:{port}/";

                var listener = new HttpListener();
                listener.Prefixes.Add(prefix);
                listener.Start();

                Console.WriteLine($"🌐 KeepAlive Server đang chạy tại {prefix}");

                // Vòng lặp nhận request và trả về phản hồi
                _ = Task.Run(async () =>
                {
                    while (listener.IsListening)
                    {
                        try
                        {
                            var context = await listener.GetContextAsync();
                            var response = context.Response;
                            var message = "✅ Bot Telegram đang chạy trên Render.com";

                            var buffer = Encoding.UTF8.GetBytes(message);
                            response.ContentLength64 = buffer.Length;
                            response.ContentType = "text/plain; charset=utf-8";

                            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                            response.OutputStream.Close();
                        }
                        catch (HttpListenerException) { break; } // Khi listener stop
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ Lỗi KeepAliveServer: {ex.Message}");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Không thể khởi động KeepAlive Server: {ex.Message}");
            }
        }
    }
}

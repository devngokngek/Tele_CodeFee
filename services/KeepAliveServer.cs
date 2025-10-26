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
            // Render cung cấp PORT qua biến môi trường
            var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
            var url = $"http://0.0.0.0:{port}/";

            using var listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();

            Console.WriteLine($"🌐 KeepAlive Server đang chạy tại {url}");

            while (true)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    var response = context.Response;

                    string message = "✅ Bot Telegram đang chạy trên Render.com";
                    byte[] buffer = Encoding.UTF8.GetBytes(message);

                    response.ContentType = "text/plain; charset=utf-8";
                    response.ContentLength64 = buffer.Length;

                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Lỗi KeepAliveServer: {ex.Message}");
                }
            }
        }
    }
}

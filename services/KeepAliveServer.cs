using System;
using System.Net;
using System.Threading.Tasks;

namespace TeleBot.Services
{
    public static class KeepAliveServer
    {
        public static async Task StartAsync()
        {
            // Render cung cấp PORT qua biến môi trường
            var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";

            var listener = new HttpListener();
            listener.Prefixes.Add($"http://*:{port}/");
            listener.Start();

            Console.WriteLine($"🌐 KeepAlive server chạy trên cổng {port}");

            // Task chạy ngầm để trả về HTTP 200 cho mọi request
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var ctx = await listener.GetContextAsync();
                    var res = ctx.Response;
                    var msg = System.Text.Encoding.UTF8.GetBytes("Bot is running ✅");
                    res.ContentLength64 = msg.Length;
                    await res.OutputStream.WriteAsync(msg);
                    res.OutputStream.Close();
                }
            });
        }
    }
}

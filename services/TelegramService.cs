using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TeleBot.Config;
using System.Threading;
using System.Threading.Tasks;
using System;
using TeleBot.Services;

namespace TeleBot.Services
{
    public class TelegramService
    {
        private readonly TelegramBotClient _bot;
        private readonly AppConfig _config;
        private CancellationTokenSource _cts;
        private readonly GoldPriceService _goldService = new();
        private readonly UserService _userService;

        // 👇 Thay bằng chatId thật của bạn (lấy bằng cách gửi /start rồi đọc log)
        private readonly long _adminChatId = 5642891542;

        public TelegramService(AppConfig config)
        {
            _config = config;
            _bot = new TelegramBotClient(config.Bot.Token);
            _userService = new UserService(_bot);
        }

        public async Task StartAsync()
        {
            try
            {
                var me = await _bot.GetMe();
                Console.WriteLine($"✅ Bot @{me.Username} đã khởi động thành công!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Không thể kết nối Telegram Bot: {ex.Message}");
                return;
            }

            _cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                _cts.Token
            );

            Console.WriteLine("🤖 Bot đang chạy... Nhấn Ctrl+C để dừng.");

            // chạy task nền cập nhật giá vàng liên tục
            _ = Task.Run(() => AutoUpdateGoldPriceAsync(_cts.Token));

            await Task.Delay(-1, _cts.Token);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken token)
        {
            if (update.Message?.Text is not string messageText)
                return;

            var chatId = update.Message.Chat.Id;
            var user = update.Message.From;
            var lower = messageText.Trim().ToLower();

            Console.WriteLine($"📩 Tin nhắn từ {chatId}: {messageText}");

            string reply;
            switch (lower)
            {
                case "/start":
                    reply = "Xin chào! Bot theo dõi giá vàng BTMC 💎\nGõ /giavang để xem giá mới nhất.";
                    break;

                case "/me":
                case "/userinfo":
                    reply = await _userService.GetUserInfoAsync(user);
                    break;
                case "/giavang":
                    reply = await _goldService.GetGoldPriceAsync();
                    break;

                case "/help":
                    reply = "📘 **Lệnh hỗ trợ:**\n" +
                            "/start - Bắt đầu\n" +
                            "/giavang - Xem giá vàng hiện tại\n" +
                            "/help - Hướng dẫn sử dụng";
                    break;

                default:
                    reply = "❓ Lệnh không hợp lệ. Gõ /help để xem danh sách lệnh.";
                    break;
            }

            await bot.SendMessage(
                chatId,
                text: reply,
                parseMode: ParseMode.Markdown,
                cancellationToken: token
            );

        }

        private async Task AutoUpdateGoldPriceAsync(CancellationToken token)
        {
            Console.WriteLine("⏳ Bắt đầu gửi giá vàng tự động mỗi 5 phút...");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var goldInfo = await _goldService.GetGoldPriceAsync();

                    await _bot.SendMessage(
                        chatId: _adminChatId,
                        text: $"💰 *Cập nhật giá vàng BTMC mới nhất:*\n\n{goldInfo}\n⏰ {DateTime.Now:HH:mm:ss dd/MM/yyyy}",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: token
                    );

                    Console.WriteLine($"✅ Đã gửi giá vàng lúc {DateTime.Now}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Lỗi cập nhật giá vàng: {ex.Message}");
                }

                // ⏰ chờ 5 phút
                await Task.Delay(TimeSpan.FromSeconds(5), token);
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken token)
        {
            Console.WriteLine($"⚠️ Lỗi bot: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}

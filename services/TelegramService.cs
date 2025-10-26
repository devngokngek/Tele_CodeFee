using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TeleBot.Config;
using System.Threading;
using System.Threading.Tasks;
using System;
using TeleBot.Services;
using Microsoft.Data.Sqlite;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleBot.Services
{
    public class TelegramService
    {
        private readonly TelegramBotClient _bot;
        private readonly AppConfig _config;
        private CancellationTokenSource _cts;
        private readonly GoldPriceService _goldService = new();
        private readonly UserService _userService;
        private readonly ComicService _comicService = new();
        private readonly GeminiService _gemini;
        private readonly DatabaseService _db = new();


        private long _adminChatId = 5642891542;

        public TelegramService(AppConfig config)
        {
            _config = config;
            _bot = new TelegramBotClient(config.Bot.Token);
            _userService = new UserService(_bot);
            _gemini = new GeminiService(config);

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
            //truyện
            _ = Task.Run(() => AutoUpdateComicAsync(_cts.Token));

            await Task.Delay(-1, _cts.Token);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken token)
        {
            try
            {
                // 🟢 1️⃣ Xử lý Callback (bấm nút InlineKeyboard)
                if (update.CallbackQuery is { } callback)
                {
                    var chatId = callback.Message.Chat.Id;
                    var data = callback.Data;

                    switch (data)
                    {
                        case "giavang":
                            var goldPrice = await _goldService.GetGoldPriceAsync();
                            await bot.SendMessage(
                                chatId,
                                text: EscapeMarkdown($"💰 Giá vàng BTMC mới nhất:\n\n{goldPrice}"),
                                parseMode: ParseMode.MarkdownV2,
                                cancellationToken: token
                            );
                            break;

                        case "userinfo":
                            var userInfo = await _userService.GetUserInfoAsync(callback.From);
                            await bot.SendMessage(
                                chatId,
                                text: EscapeMarkdown(userInfo),
                                parseMode: ParseMode.MarkdownV2,
                                cancellationToken: token
                            );
                            break;

                        case "help":
                            string helpText =
                                "📘 *Hướng dẫn sử dụng:*\n\n" +
                                "• /start – Hiển thị menu chính\n" +
                                "• /giavang – Xem giá vàng mới nhất\n" +
                                "• /userinfo – Xem thông tin của bạn\n\n" +
                                "💡 Bạn cũng có thể bấm các nút trong menu để thao tác nhanh.";
                            await bot.SendMessage(
                                chatId,
                                text: EscapeMarkdown(helpText),
                                parseMode: ParseMode.MarkdownV2,
                                cancellationToken: token
                            );
                            break;
                    }

                    await bot.AnswerCallbackQuery(callback.Id);
                    return;
                }

                // 🟡 2️⃣ Nếu là tin nhắn văn bản
                if (update.Message?.Text is not string messageText)
                    return;

                var chatIdText = update.Message.Chat.Id;
                var user = update.Message.From;
                var lower = messageText.Trim().ToLower();

                await _db.AddUserAsync(chatIdText, user?.Username);
                Console.WriteLine($"📩 Tin nhắn từ {chatIdText}: {messageText}");

                string reply;

                switch (lower)
                {
                    // 🏁 /start
                    case "/start":
                        string menuText =
        $@"🌟 *Chào mừng {EscapeMarkdown(user?.FirstName ?? "bạn")} đến với BOT Giá Vàng BTMC!* 💎

📊 Theo dõi *giá vàng BTMC* mới nhất  
👤 Xem thông tin người dùng của bạn  
ℹ️ Hướng dẫn sử dụng bot

Chọn một chức năng bên dưới 👇";

                        var menuKeyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("💰 Xem giá vàng", "giavang"),
                                InlineKeyboardButton.WithCallbackData("👤 Thông tin cá nhân", "userinfo")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("📘 Trợ giúp", "help"),
                                InlineKeyboardButton.WithUrl("🌐 Website BTMC", "https://btmc.vn")
                            }
                        });

                        await bot.SendMessage(
                            chatIdText,
                            text: EscapeMarkdown(menuText),
                            parseMode: ParseMode.MarkdownV2,
                            replyMarkup: menuKeyboard,
                            cancellationToken: token
                        );
                        break;

                    // 👤 /userinfo
                    case "/me":
                    case "/userinfo":
                        reply = await _userService.GetUserInfoAsync(user);
                        await bot.SendMessage(chatIdText, EscapeMarkdown(reply), ParseMode.MarkdownV2, cancellationToken: token);
                        break;

                    // 💰 /giavang
                    case "/giavang":
                        reply = await _goldService.GetGoldPriceAsync();

                        // Gửi admin log
                        string adminText =
                            $"💰 *Cập nhật giá vàng BTMC mới nhất:*\n\n{reply}\n⏰ {DateTime.Now:HH\\:mm\\:ss dd\\/MM\\/yyyy}";
                        await _bot.SendMessage(
                            _adminChatId,
                            text: EscapeMarkdown(adminText),
                            parseMode: ParseMode.MarkdownV2,
                            cancellationToken: token
                        );

                        // Gửi người dùng
                        await bot.SendMessage(
                            chatIdText,
                            text: EscapeMarkdown($"💰 Giá vàng BTMC mới nhất:\n\n{reply}"),
                            parseMode: ParseMode.MarkdownV2,
                            cancellationToken: token
                        );
                        break;

                    // 📘 /help
                    case "/help":
                        reply =
                            "📘 *Lệnh hỗ trợ:*\n" +
                            "/start - Bắt đầu\n" +
                            "/giavang - Xem giá vàng hiện tại\n" +
                            "/userinfo - Thông tin người dùng\n" +
                            "/help - Hướng dẫn sử dụng";
                        await bot.SendMessage(chatIdText, EscapeMarkdown(reply), ParseMode.MarkdownV2, cancellationToken: token);
                        break;

                    // 👥 /users
                    case "/users":
                        int total = await _db.CountUsersAsync();
                        reply = $"👋 Chào {EscapeMarkdown(user?.FirstName ?? "bạn")}! Bạn là người dùng thứ {total} của bot này.";
                        await bot.SendMessage(chatIdText, EscapeMarkdown(reply), ParseMode.MarkdownV2, cancellationToken: token);
                        break;

                    // ⚙️ /update
                    case string msg when msg.StartsWith("/update"):
                        if (chatIdText != 5642891542)
                        {
                            reply = "🚫 Lệnh này chỉ dành cho admin.";
                            break;
                        }

                        var parts = msg.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 3)
                        {
                            reply = "⚙️ Cú pháp đúng:\n`/update <chatId> <username>`\n\nVí dụ:\n`/update 123456789 john_doe`";
                            break;
                        }

                        if (!long.TryParse(parts[1], out long targetChatId))
                        {
                            reply = "❌ ChatId không hợp lệ. Vui lòng nhập số.";
                            break;
                        }

                        string newUsername = parts[2].Trim();
                        using (var connection = new SqliteConnection("Data Source=data.db"))
                        {
                            await connection.OpenAsync();
                            var checkCmd = connection.CreateCommand();
                            checkCmd.CommandText = "SELECT COUNT(*) FROM Users WHERE ChatId = $chatId";
                            checkCmd.Parameters.AddWithValue("$chatId", targetChatId);
                            long count = (long)await checkCmd.ExecuteScalarAsync();

                            if (count == 0)
                            {
                                reply = $"⚠️ Không tìm thấy người dùng có ChatId = {targetChatId}.";
                                break;
                            }

                            var updateCmd = connection.CreateCommand();
                            updateCmd.CommandText = "UPDATE Users SET Username = $username WHERE ChatId = $chatId";
                            updateCmd.Parameters.AddWithValue("$username", newUsername);
                            updateCmd.Parameters.AddWithValue("$chatId", targetChatId);
                            int rows = await updateCmd.ExecuteNonQueryAsync();
                            reply = rows > 0
                                ? $"✅ Đã cập nhật username của ChatId `{targetChatId}` thành `{newUsername}`."
                                : "⚠️ Cập nhật thất bại, vui lòng thử lại.";
                        }
                        await bot.SendMessage(chatIdText, EscapeMarkdown(reply), ParseMode.MarkdownV2, cancellationToken: token);
                        break;

                    // 📢 /sendall
                    case string msg when msg.StartsWith("/sendall"):
                        if (chatIdText != 5642891542)
                        {
                            reply = "🚫 Lệnh này chỉ dành cho admin.";
                            break;
                        }

                        string textToSend = msg.Replace("/sendall", "").Trim();
                        if (string.IsNullOrEmpty(textToSend))
                        {
                            reply = "📢 Nhập nội dung sau lệnh. Ví dụ:\n`/sendall Chào mọi người!`";
                        }
                        else
                        {
                            await BroadcastMessageAsync(textToSend);
                            reply = "✅ Đã gửi tin nhắn đến tất cả người dùng.";
                        }

                        await bot.SendMessage(chatIdText, EscapeMarkdown(reply), ParseMode.MarkdownV2, cancellationToken: token);
                        break;

                    // 🤖 Chat AI
                    default:
                        await bot.SendChatAction(chatIdText, ChatAction.Typing, cancellationToken: token);
                        reply = await _gemini.AskAsync(messageText);
                        await bot.SendMessage(chatIdText, EscapeMarkdown(reply), ParseMode.MarkdownV2, cancellationToken: token);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Lỗi bot: {ex.Message}");
            }
        }

        private static string EscapeMarkdown1(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Thoát các ký tự đặc biệt trong MarkdownV2
            var specialChars = new[] { "_", "*", "[", "]", "(", ")", "~", "`", ">", "#", "+", "-", "=", "|", "{", "}", ".", "!" };
            foreach (var ch in specialChars)
                text = text.Replace(ch, "\\" + ch);

            return text;
        }
        private static string EscapeMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            text = text.Replace("\\", "\\\\"); // escape backslash trước
            string[] specials = { "_", "*", "[", "]", "(", ")", "~", "`", ">", "#", "+", "-", "=", "|", "{", "}", ".", "!" };
            foreach (var ch in specials)
                text = text.Replace(ch, "\\" + ch);
            return text;
        }

        private async Task AutoUpdateGoldPriceAsync(CancellationToken token)
        {
            Console.WriteLine("⏳ Bắt đầu tự động cập nhật giá vàng...");

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
                await Task.Delay(TimeSpan.FromHours(1), token);
            }
        }
        // auto load truyen service
        private async Task AutoUpdateComicAsync(CancellationToken token)
        {
            Console.WriteLine("⏳ Bắt đầu tự động lấy thông tin truyện...");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var info = await _comicService.GetComicInfoAsync();

                    await _bot.SendMessage(
                        chatId: _adminChatId,
                        text: info,
                        parseMode: ParseMode.Markdown,
                        cancellationToken: token
                    );

                    Console.WriteLine($"✅ Đã gửi thông tin truyện lúc {DateTime.Now:HH:mm:ss}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Lỗi khi lấy truyện': {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), token);

                // ⏰ Lặp lại sau 1 giờ
                await Task.Delay(TimeSpan.FromHours(1), token);
            }
        }
        private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken token)
        {
            Console.WriteLine($"⚠️ Lỗi bot: {ex.Message}");
            return Task.CompletedTask;
        }
        //send to all user
        public async Task BroadcastMessageAsync(string message)
        {
            try
            {
                var userIds = await _db.GetAllUserIdsAsync();
                foreach (var chatId in userIds)
                {
                    try
                    {
                        await _bot.SendMessage(chatId, message);
                        await Task.Delay(1000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Không gửi được tới {chatId}: {ex.Message}");
                    }
                }

                Console.WriteLine("✅ Hoàn tất gửi tin hàng loạt.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khi gửi broadcast: {ex.Message}");
            }
        }

    }

}

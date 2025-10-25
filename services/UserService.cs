using Telegram.Bot;
using Telegram.Bot.Types;
using System;
using System.Threading.Tasks;

namespace TeleBot.Services
{
    public class UserService
    {
        private readonly TelegramBotClient _bot;

        public UserService(TelegramBotClient bot)
        {
            _bot = bot;
        }

        // ✅ Hàm lấy thông tin đầy đủ người dùng đang chat
        public async Task<string> GetUserInfoAsync(User user)
        {
            if (user == null) return "Không có thông tin user.";

            var info = $"👤 *Thông tin người dùng Telegram:*\n\n" +
                       $"🆔 ID: `{user.Id}`\n" +
                       $"👨‍💼 Tên: {user.FirstName} {user.LastName}\n" +
                       $"💬 Username: @{user.Username ?? "(không có)"}\n" +
                       $"🌍 Ngôn ngữ: {user.LanguageCode ?? "Không rõ"}\n" +
                       $"🤖 Là bot: {(user.IsBot ? "✅ Có" : "❌ Không")}\n";

            return info;
        }
    }
}

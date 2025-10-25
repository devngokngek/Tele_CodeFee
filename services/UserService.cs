using Telegram.Bot;
using Telegram.Bot.Types;
using System;
using System.Threading.Tasks;
using System.Text;

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
            if (user == null)
                return "❌ Không có thông tin người dùng.";

            var now = DateTime.UtcNow.AddHours(7);
            var joinEmoji = user.IsBot ? "🤖" : "🧍";

            var info =
            $"╭─── ✨ *THÔNG TIN TELEGRAM* ✨ ───╮\n" +
            $"├ 🆔 *ID:* `{user.Id}`\n" +
            $"├ 👤 *Tên:* {EscapeMarkdownV2(user.FirstName + " " + (user.LastName ?? ""))}\n" +
            $"{(string.IsNullOrEmpty(user.Username) ? "" : $"├ 💬 *Username:* @{EscapeMarkdownV2(user.Username)}\n")}" +
            $"├ 🌍 *Ngôn ngữ:* {user.LanguageCode?.ToUpper() ?? "Không rõ"}\n" +
            $"├ 🤖 *Là bot:* {(user.IsBot ? "✅ Có" : "❌ Không")}\n" +
            $"├ 💎 *Premium:* {(user.IsPremium ? "🌟 Có" : "🚫 Không")}\n" +
            $"├ 🕒 *Thời gian:* {DateTime.Now:HH:mm:ss dd/MM/yyyy}\n" +
            $"├ 🔗 *Liên kết:* [Nhấn để nhắn tin](tg://user?id={user.Id})\n" +
            $"╰─────────────────────────────────╯\n\n" +
            $"✨ _Thông tin được cung cấp bởi bot_";

            string EscapeMarkdownV2(string text)
            {
                if (string.IsNullOrEmpty(text)) return text;

                char[] specialChars = { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' };
                StringBuilder result = new StringBuilder();

                foreach (char c in text)
                {
                    if (Array.Exists(specialChars, x => x == c))
                        result.Append('\\');
                    result.Append(c);
                }

                return result.ToString();
            }

            return info;
        }
        public User? GetUserFromUpdate(Update update)
        {
            if (update == null)
                return null;

            // Ưu tiên lấy từ Message
            if (update.Message?.From != null)
                return update.Message.From;

            // Nếu là CallbackQuery (nút bấm inline)
            if (update.CallbackQuery?.From != null)
                return update.CallbackQuery.From;

            // Nếu là InlineQuery (khi user tìm bot inline)
            if (update.InlineQuery?.From != null)
                return update.InlineQuery.From;

            // Nếu là ChatMemberUpdated (khi join/leave)
            if (update.MyChatMember?.From != null)
                return update.MyChatMember.From;

            return null;
        }

        // ✅ Hàm lấy ChatId của người đang chat (rất tiện)
        public long? GetChatIdFromUpdate(Update update)
        {
            if (update?.Message?.Chat != null)
                return update.Message.Chat.Id;

            if (update?.CallbackQuery?.Message?.Chat != null)
                return update.CallbackQuery.Message.Chat.Id;

            if (update?.InlineQuery?.From != null)
                return update.InlineQuery.From.Id;

            if (update?.MyChatMember?.Chat != null)
                return update.MyChatMember.Chat.Id;

            return null;
        }

        // 👉 Hàm escape MarkdownV2 (dùng nội bộ)
        private static string EscapeMarkdownV2(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            char[] specialChars = { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' };
            StringBuilder result = new StringBuilder();

            foreach (char c in text)
            {
                if (Array.Exists(specialChars, x => x == c))
                    result.Append('\\');
                result.Append(c);
            }

            return result.ToString();
        }
    }
}

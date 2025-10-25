using Telegram.Bot;
using Telegram.Bot.Types;
using TeleBot.Config;

namespace TeleBot.Handlers
{
    public static class MessageHandler
    {
        public static async Task HandleMessage(ITelegramBotClient bot, Message msg, AppConfig config, CancellationToken token)
        {
            if (msg.Text is null) return;

            string text = msg.Text.ToLower();
            string reply = text switch
            {
                "/start" => "Xin chào! Tôi là bot của bạn 🚀",
                "hi" => "Chào bạn 👋",
                _ => $"Bạn vừa nói: {msg.Text}"
            };

            await bot.SendMessage(msg.Chat.Id, reply, cancellationToken: token);
        }
    }
}

using Telegram.Bot;
using Telegram.Bot.Types;
using TeleBot.Config;
using System.Threading;
using System.Threading.Tasks;

namespace TeleBot.Handlers
{
    public static class CallbackHandler
    {
        public static async Task HandleCallback(ITelegramBotClient bot, CallbackQuery callback, AppConfig config, CancellationToken token)
        {
            if (callback.Data == null) return;

            // Ví dụ xử lý callback inline button
            await bot.AnswerCallbackQuery(callback.Id, $"Bạn vừa chọn: {callback.Data}", cancellationToken: token);
            await bot.SendMessage(callback.Message.Chat.Id, $"👉 Callback nhận được: {callback.Data}", cancellationToken: token);
        }
    }
}

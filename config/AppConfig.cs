using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TeleBot.Config
{
    public class AppConfig
    {
        public BotConfig Bot { get; set; } = new();
        public AppSettings App { get; set; } = new();

        public static AppConfig Load(string path)
        {
            var config = new AppConfig();

            // 🧩 1️⃣ Đọc từ biến môi trường (Render / Docker / Server)
            var tokenFromEnv = Environment.GetEnvironmentVariable("Bot__Token");
            var adminIdsFromEnv = Environment.GetEnvironmentVariable("Bot__AdminIds");
            var environmentFromEnv = Environment.GetEnvironmentVariable("App__Environment");
            var logLevelFromEnv = Environment.GetEnvironmentVariable("App__LogLevel");

            if (!string.IsNullOrEmpty(tokenFromEnv))
            {
                config.Bot.Token = tokenFromEnv;
            }

            if (!string.IsNullOrEmpty(adminIdsFromEnv))
            {
                // Cho phép nhiều ID, phân cách bằng dấu phẩy
                config.Bot.AdminIds = adminIdsFromEnv
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => long.TryParse(id.Trim(), out var parsed) ? parsed : 0)
                    .Where(id => id != 0)
                    .ToList();
            }

            if (!string.IsNullOrEmpty(environmentFromEnv))
                config.App.Environment = environmentFromEnv;

            if (!string.IsNullOrEmpty(logLevelFromEnv))
                config.App.LogLevel = logLevelFromEnv;

            // 🧩 2️⃣ Nếu chạy local (có file appsettings.json) → đọc thêm
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var fileConfig = JsonSerializer.Deserialize<AppConfig>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // chỉ gán nếu chưa có từ Env
                if (string.IsNullOrEmpty(config.Bot.Token))
                    config.Bot.Token = fileConfig.Bot.Token;

                if (config.Bot.AdminIds == null || !config.Bot.AdminIds.Any())
                    config.Bot.AdminIds = fileConfig.Bot.AdminIds;

                if (string.IsNullOrEmpty(config.App.Environment))
                    config.App.Environment = fileConfig.App.Environment;

                if (string.IsNullOrEmpty(config.App.LogLevel))
                    config.App.LogLevel = fileConfig.App.LogLevel;
            }

            return config;
        }
    }

    public class BotConfig
    {
        public string Token { get; set; }
        public List<long> AdminIds { get; set; } = new();
    }

    public class AppSettings
    {
        public string Environment { get; set; } = "Development";
        public string LogLevel { get; set; } = "Information";
    }
}

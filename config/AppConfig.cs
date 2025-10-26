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
        public GeminiConfig Gemini { get; set; } = new();

        public static AppConfig Load(string path)
        {
            var config = new AppConfig();

            var tokenFromEnv = Environment.GetEnvironmentVariable("BOT_TOKEN");
            var geminiFromEnv = Environment.GetEnvironmentVariable("GEM_TOKEN");
            var adminIdsFromEnv = Environment.GetEnvironmentVariable("Bot__AdminIds");
            var environmentFromEnv = Environment.GetEnvironmentVariable("App__Environment");
            var logLevelFromEnv = Environment.GetEnvironmentVariable("App__LogLevel");

            if (!string.IsNullOrEmpty(tokenFromEnv))
            {
                config.Bot.Token = tokenFromEnv;
            }
            if (!string.IsNullOrEmpty(geminiFromEnv))
            {
                config.Gemini.ApiKey = geminiFromEnv;
            }

            if (!string.IsNullOrEmpty(adminIdsFromEnv))
            {
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

            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var fileConfig = JsonSerializer.Deserialize<AppConfig>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (string.IsNullOrEmpty(config.Bot.Token))
                    config.Bot.Token = fileConfig.Bot.Token;

                if (config.Bot.AdminIds == null || !config.Bot.AdminIds.Any())
                    config.Bot.AdminIds = fileConfig.Bot.AdminIds;

                if (string.IsNullOrEmpty(config.App.Environment))
                    config.App.Environment = fileConfig.App.Environment;

                if (string.IsNullOrEmpty(config.App.LogLevel))
                    config.App.LogLevel = fileConfig.App.LogLevel;
                if (string.IsNullOrEmpty(config.Gemini.ApiKey))
                    config.Gemini.ApiKey = fileConfig.Gemini.ApiKey;
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
        public class GeminiConfig
    {
        public string ApiKey { get; set; } = "";
    }
}

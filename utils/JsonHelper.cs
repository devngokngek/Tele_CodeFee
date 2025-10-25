using System.IO;
using System.Text.Json;

namespace TeleBot.Utils
{
    public static class JsonHelper
    {
        public static T Load<T>(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException(path);
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json);
        }

        public static void Save<T>(string path, T data)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}

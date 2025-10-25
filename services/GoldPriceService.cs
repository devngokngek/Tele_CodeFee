using System;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TeleBot.Services
{
    public class GoldPriceService
    {
        private const string ApiUrl = "http://api.btmc.vn/api/BTMCAPI/getpricebtmc?key=3kd8ub1llcg9t45hnoh8hmn7t5kc2v";

        public async Task<string> GetGoldPriceAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "C# TelegramBot");

                var response = await client.GetStringAsync(ApiUrl);
                using var doc = JsonDocument.Parse(response);

                var dataArray = doc.RootElement
                    .GetProperty("DataList")
                    .GetProperty("Data");

                var items = new List<(string Name, string Buy, string Sell, string Date)>();

                foreach (var item in dataArray.EnumerateArray())
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var prop in item.EnumerateObject())
                        dict[prop.Name] = prop.Value.GetString();

                    string name = GetValue(dict, "@n_");
                    string buy = FormatPrice(GetValue(dict, "@pb_"));
                    string sell = FormatPrice(GetValue(dict, "@ps_"));
                    string date = GetValue(dict, "@d_");

                    if (!string.IsNullOrEmpty(name))
                        items.Add((name, buy, sell, date));
                }

                if (items.Count == 0)
                    return "⚠️ Không có dữ liệu giá vàng.";

                // Dùng ngày đầu tiên làm ngày chung
                string dateInfo = items[0].Date;

                string result = "💎 *Giá vàng BTMC hôm nay* 💎\n" +
                                $"📅 _Cập nhật: {EscapeMarkdown(dateInfo)}_\n\n" +
                                "━━━━━━━━━━━━━━━━━━\n\n";

                foreach (var item in items)
                {
                    result += $"🏷 *{EscapeMarkdown(item.Name)}*\n" +
                              $"💰 Mua: `{item.Buy}` | Bán: `{item.Sell}`\n\n";
                }

                result += "━━━━━━━━━━━━━━━━━━\n" +
                          "📊 _Nguồn: BTMC.vn_";

                return result;
            }
            catch (Exception ex)
            {
                return $"⚠️ Lỗi khi lấy dữ liệu: {ex.Message}";
            }
        }

        private static string GetValue(Dictionary<string, string> dict, string prefix)
        {
            foreach (var kv in dict)
                if (kv.Key.StartsWith(prefix))
                    return kv.Value;
            return "";
        }

        private static string FormatPrice(string value)
        {
            if (decimal.TryParse(value, out var num))
                return num.ToString("#,##0 ₫", CultureInfo.InvariantCulture)
                    .Replace(",", ".");
            return "—";
        }

        // Escape các ký tự đặc biệt cho MarkdownV2 Telegram
        private static string EscapeMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return Regex.Replace(text, @"([_\*\[\]\(\)~`>#+\-=|{}\.!])", @"\$1");
        }
    }
}

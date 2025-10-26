using System;
using System.Net.Http;
using System.Text.Json;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;

namespace TeleBot.Services
{
    public class ComicService
    {
        public async Task<string> GetComicInfoAsync()
        {
            var comics = new List<string>
            {
                "Nga Lão Ma Thần",
                "Sát Thủ Peter", 
                "Dai quan gia la ma hoang"
            };

            var sb = new StringBuilder();
            int updatedCount = 0;

            foreach (var title in comics)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        sb.AppendLine("❌ Bạn chưa nhập tên truyện.");
                        continue;
                    }

                    string slug = ToSlug(title);
                    string apiUrl = $"https://otruyenapi.com/v1/api/truyen-tranh/{slug}?load=chapters";

                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("User-Agent", "TelegramBot");
                    client.Timeout = TimeSpan.FromSeconds(30);

                    var json = await client.GetStringAsync(apiUrl);
                    using var doc = JsonDocument.Parse(json);

                    var item = doc.RootElement
                        .GetProperty("data")
                        .GetProperty("item");

                    string name = item.GetProperty("name").GetString() ?? "(Không tên)";
                    string status = item.GetProperty("status").GetString() ?? "(Không rõ)";
                    string thumb = item.GetProperty("thumb_url").GetString() ?? "";
                    string author = item.GetProperty("author")[0].GetString() ?? "Đang cập nhật";

                    DateTime updatedAt = item.TryGetProperty("updatedAt", out var upd)
                        ? upd.GetDateTime()
                        : DateTime.MinValue;
                    
                    string updatedText = updatedAt != DateTime.MinValue 
                        ? updatedAt.ToString("dd/MM/yyyy HH:mm")
                        : "(không rõ)";

                    // Sửa lỗi: So sánh ngày thay vì parse string
                    bool isUpdatedToday = updatedAt.Date == DateTime.Today;
                    //DateTime specificDate = new DateTime(2025, 10, 25);
                    //Console.WriteLine(updatedAt.Date);
                    //Console.WriteLine("a:" + specificDate);

                    //bool isUpdatedToday = updatedAt.Date == specificDate.Date;

                    if (isUpdatedToday)
                    {
                        var chapters = item.GetProperty("chapters")[0]
                            .GetProperty("server_data")
                            .EnumerateArray()
                            .Select(ch => ch.GetProperty("chapter_name").GetString())
                            .Take(5)
                            .ToList();

                        string chapList = string.Join("\n", chapters.Select(c => $"📄 {c}"));

                        sb.AppendLine(
                            $"🎉 *CẬP NHẬT MỚI HÔM NAY* 🎉\n" +
                            $"🌟 **{EscapeMarkdownV2(name.ToUpper())}** 🌟\n" +
                            $"┌─ 🖊️ **Tác giả:** {EscapeMarkdownV2(author)}\n" +
                            $"├─ 🔄 **Trạng thái:** {GetStatusEmoji(status)} {EscapeMarkdownV2(status)}\n" +
                            $"├─ ⏰ **Cập nhật:** {EscapeMarkdownV2(updatedText)} 🆕\n" +
                            $"├─ 🎨 **[Xem ảnh bìa]({thumb})**\n" +
                            $"└─ 💫 **[Đọc truyện ngay!](https://otruyen.com/truyen-tranh/{slug})**\n\n" +
                            
                            //$"📚 *Chương mới:*\n{chapList}\n\n" +
                            
                            $"🎊 *Truyện vừa được cập nhật hôm nay!*\n" +
                            $"🎆────────────────────────🎆\n"
                        );
                        updatedCount++;
                    }
                    else
                    {
                        sb.AppendLine($"ℹ️ Truyện {name} chưa có cập nhật hôm nay.\n");
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    sb.AppendLine($"🌐 Lỗi kết nối khi tải truyện '{title}': {httpEx.Message}\n");
                }
                catch (JsonException jsonEx)
                {
                    sb.AppendLine($"📄 Lỗi dữ liệu truyện '{title}': {jsonEx.Message}\n");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"⚠️ Lỗi khi tải truyện '{title}': {ex.Message}\n");
                }
            }

            if (updatedCount == 0)
            {
                sb.Insert(0, "📊 *KHÔNG CÓ TRUYỆN NÀO CẬP NHẬT HÔM NAY*\n\n");
            }
            else
            {
                sb.Insert(0, $"📊 *CÓ {updatedCount} TRUYỆN CẬP NHẬT HÔM NAY*\n\n");
            }

            return sb.ToString();
        }
        private string GetStatusEmoji(string status)
        {
            return status.ToLower() switch
            {
                "ongoing" or "đang ra" => "🟢",
                "completed" or "hoàn thành" => "✅",
                "dropped" or "tạm ngưng" => "⏸️",
                _ => "📖"
            };
        }

        private string FormatDescription(string desc)
        {
            if (string.IsNullOrEmpty(desc)) return "_Chưa có mô tả_";

            var cleanDesc = Regex.Replace(desc, "<.*?>", string.Empty);
            if (cleanDesc.Length > 300)
                cleanDesc = cleanDesc.Substring(0, 300) + "...";

            return EscapeMarkdownV2(cleanDesc);
        }

        private string FormatChapterList(string chapList)
        {
            if (string.IsNullOrEmpty(chapList)) return "_Chưa có chương nào_";

            var chapters = chapList.Split('\n')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Take(5)
                .ToArray();

            if (chapters.Length == 0) return "_Chưa có chương nào_";

            var result = new StringBuilder();
            for (int i = 0; i < chapters.Length; i++)
            {
                var emoji = i switch { 0 => "🆕", 1 => "🔥", _ => "📄" };
                result.AppendLine($"{emoji} {EscapeMarkdownV2(chapters[i].Trim())}");
            }

            return result.ToString();
        }

        private string EscapeMarkdownV2(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            char[] specialChars = { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' };
            var result = new StringBuilder();

            foreach (char c in text)
            {
                if (Array.Exists(specialChars, x => x == c))
                    result.Append('\\');
                result.Append(c);
            }

            return result.ToString();
        }

        private static string ToSlug(string text)
        {
            text = text.ToLower();
            text = text.Normalize(NormalizationForm.FormD);
            text = Regex.Replace(text, @"\p{IsCombiningDiacriticalMarks}+", "");
            text = Regex.Replace(text, @"[^a-z0-9\s-]", "");
            text = Regex.Replace(text, @"\s+", "-").Trim('-');
            return text;
        }
    }
}

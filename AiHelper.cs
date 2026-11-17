using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace one_circle
{
    public class AiHelper
    {
        private const string ApiKey = " key ";
        private readonly List<(string Role, string Text)> history = new();
        private static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(15) };
        private string city = "ваш город";

        public View BuildAiUI(Activity a, bool dark, Action onBack)
        {
            var prefs = a.GetSharedPreferences("oc_auth", FileCreationMode.Private);
            string nick = prefs?.GetString("nick", "Друг") ?? "Друг", birth = prefs?.GetString("birth", "не указана") ?? "не указана";
            var root = new LinearLayout(a) { Orientation = Orientation.Vertical, LayoutParameters = new LinearLayout.LayoutParams(-1, -1) };
            root.SetBackgroundColor(Color.ParseColor(dark ? "#121216" : "#F5F7FA"));

            var top = new LinearLayout(a) { Orientation = Orientation.Horizontal }; top.SetGravity(GravityFlags.CenterVertical); top.SetPadding(18, 14, 18, 14); top.SetBackgroundColor(Color.ParseColor(dark ? "#1E1E24" : "#E2E8F0"));
            var btnBack = Btn(a, "⬅️ Назад", "#DC2626", "#FFFFFF");
            var lbl = new TextView(a) { Text = "🤖 DeepSeek ИИ", TextSize = 15, LayoutParameters = new LinearLayout.LayoutParams(0, -2, 1f) }; lbl.SetTypeface(null, TypefaceStyle.Bold); lbl.SetTextColor(Color.ParseColor(dark ? "#FFFFFF" : "#1E293B")); lbl.SetPadding(16, 0, 0, 0);
            var btnInfo = Btn(a, "ℹ️", "#475569", "#FFFFFF", 11, 18, 10);
            top.AddView(btnBack); top.AddView(lbl); top.AddView(btnInfo); root.AddView(top);

            var scroll = new ScrollView(a) { LayoutParameters = new LinearLayout.LayoutParams(-1, 0, 1f) };
            var list = new LinearLayout(a) { Orientation = Orientation.Vertical }; list.SetPadding(24, 20, 24, 20); scroll.AddView(list); root.AddView(scroll);
            int maxW = (int)((a.Resources?.DisplayMetrics?.WidthPixels ?? 800) * 0.82);

            void Render() => a.RunOnUiThread(() => {
                list.RemoveAllViews();
                if (history.Count == 0) { var em = new TextView(a) { Text = $"Привет, {nick}! 📅 {DateTime.Now:dd.MM.yyyy}\n📍 {city}\n\nВыберите запрос или напишите свой вопрос.", TextSize = 14, Gravity = GravityFlags.Center }; em.SetTextColor(Color.ParseColor("#94A3B8")); em.SetPadding(20, 80, 20, 0); list.AddView(em); return; }
                foreach (var m in history)
                {
                    bool u = m.Role == "user"; var row = new LinearLayout(a) { Orientation = Orientation.Vertical }; row.SetGravity(u ? GravityFlags.End : GravityFlags.Start);
                    var lp = new LinearLayout.LayoutParams(-1, -2); lp.SetMargins(0, 0, 0, 14); row.LayoutParameters = lp;
                    var b = new TextView(a) { Text = u ? m.Text : $"DeepSeek:\n{m.Text}", TextSize = 15, LayoutParameters = new LinearLayout.LayoutParams(-2, -2) };
                    b.SetMaxWidth(maxW); b.SetPadding(26, 16, 26, 16); b.SetTextColor(Color.ParseColor(u || dark ? "#FFFFFF" : "#1E293B"));
                    var gd = new GradientDrawable(); gd.SetColor(Color.ParseColor(u ? "#2563EB" : (dark ? "#27272A" : "#FFFFFF"))); gd.SetCornerRadius(24f); b.Background = gd;
                    row.AddView(b); list.AddView(row);
                }
                scroll.Post(() => scroll.FullScroll(FocusSearchDirection.Down));
            });

            _ = Task.Run(async () => { city = await GetCity(a); Render(); });
            async Task Send(string q) { history.Add(("user", q)); history.Add(("assistant", "⏳ Отвечаю...")); Render(); city = await GetCity(a); history[^1] = ("assistant", await Ask(q, nick, birth)); Render(); }
            Render();

            var hScroll = new HorizontalScrollView(a) { HorizontalScrollBarEnabled = false };
            var chips = new LinearLayout(a) { Orientation = Orientation.Horizontal }; chips.SetPadding(16, 8, 16, 8);
            var q1 = Btn(a, "📅 Праздники", dark ? "#334155" : "#E2E8F0", dark ? "#FFFFFF" : "#1E293B", 11, 18, 10);
            var q2 = Btn(a, "📰 Новости города", dark ? "#334155" : "#E2E8F0", dark ? "#FFFFFF" : "#1E293B", 11, 18, 10);
            var q3 = Btn(a, "🎂 Мой профиль", dark ? "#334155" : "#E2E8F0", dark ? "#FFFFFF" : "#1E293B", 11, 18, 10);
            var lp2 = new LinearLayout.LayoutParams(-2, -2); lp2.SetMargins(10, 0, 10, 0); q2.LayoutParameters = lp2;
            chips.AddView(q1); chips.AddView(q2); chips.AddView(q3); hScroll.AddView(chips); root.AddView(hScroll);

            q1.Click += async (s, e) => await Send("Какие сегодня праздники в России?");
            q2.Click += async (s, e) => { city = await GetCity(a); await Send($"Добрые новости и факты о моём месте ({city}) и России."); };
            q3.Click += async (s, e) => await Send($"Дата рождения {birth}. Сколько полных лет на {DateTime.Now:dd.MM.yyyy}, сколько дней до дня рождения и семейный совет?");

            var bar = new LinearLayout(a) { Orientation = Orientation.Horizontal }; bar.SetPadding(16, 10, 16, 16); bar.SetBackgroundColor(Color.ParseColor(dark ? "#1E1E24" : "#E2E8F0"));
            var inp = new EditText(a) { Hint = "Спросить DeepSeek...", TextSize = 15 }; inp.SetPadding(26, 16, 26, 16); inp.SetTextColor(Color.ParseColor(dark ? "#FFFFFF" : "#1E293B")); inp.SetHintTextColor(Color.ParseColor("#94A3B8"));
            var gdI = new GradientDrawable(); gdI.SetColor(Color.ParseColor(dark ? "#27272A" : "#FFFFFF")); gdI.SetCornerRadius(24f); inp.Background = gdI;
            var lpI = new LinearLayout.LayoutParams(0, -2, 1f); lpI.SetMargins(0, 0, 12, 0); inp.LayoutParameters = lpI;
            var btnSend = Btn(a, "➤", "#7C3AED", "#FFFFFF", 16, 28, 14);
            bar.AddView(inp); bar.AddView(btnSend); root.AddView(bar);

            btnSend.Click += async (s, e) => { string q = inp.Text?.Trim() ?? ""; if (q != "") { inp.Text = ""; await Send(q); } };
            btnInfo.Click += (s, e) => new AlertDialog.Builder(a).SetTitle("Прозрачность ИИ").SetMessage($"• Модель: DeepSeek.\n• Город ({city}) определяется по геометке.\n• Координаты GPS и переписка в ИИ не передаются.").SetPositiveButton("ОК", (s2, e2) => { }).Show();
            btnBack.Click += (s, e) => onBack();
            return root;
        }

        private async Task<string> GetCity(Activity a)
        {
            try
            {
                var lp = a.GetSharedPreferences("oc_loc", FileCreationMode.Private);
                string? la = lp?.GetString("lat", ""), lo = lp?.GetString("lon", "");
                if (!string.IsNullOrEmpty(la) && !string.IsNullOrEmpty(lo))
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, "https" + "://" + $"nominatim.openstreetmap.org/reverse?format=json&lat={la}&lon={lo}&accept-language=ru");
                    req.Headers.UserAgent.ParseAdd("OnesCircle/1.0");
                    using var res = await http.SendAsync(req);
                    if (res.IsSuccessStatusCode)
                    {
                        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                        if (doc.RootElement.TryGetProperty("address", out var ad))
                        {
                            string t = ad.TryGetProperty("city", out var c) ? c.GetString() ?? "" : (ad.TryGetProperty("town", out var tw) ? tw.GetString() ?? "" : (ad.TryGetProperty("village", out var v) ? v.GetString() ?? "" : ""));
                            string st = ad.TryGetProperty("state", out var s) ? s.GetString() ?? "" : "";
                            string f = $"{t}, {st}".Trim(' ', ',');
                            if (!string.IsNullOrWhiteSpace(f)) return f;
                        }
                    }
                }
            }
            catch { }
            return city;
        }

        private async Task<string> Ask(string q, string nick, string birth)
        {
            try
            {
                string sys = $"Ты семейный ИИ one's circle. Сегодня {DateTime.Now:dd.MM.yyyy}. Пользователь {nick}, дата рождения {birth}, город {city}. Отвечай кратко (3 предложения), без звездочек и решеток.";
                string body = $"{{\"model\":\"deepseek-chat\",\"max_tokens\":250,\"messages\":[{{\"role\":\"system\",\"content\":\"{JsonEncodedText.Encode(sys)}\"}},{{\"role\":\"user\",\"content\":\"{JsonEncodedText.Encode(q)}\"}}]}}";
                using var req = new HttpRequestMessage(HttpMethod.Post, "https" + "://" + "api.deepseek" + ".com/chat/completions");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");
                using var res = await http.SendAsync(req);
                string json = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return $"Ошибка API ({res.StatusCode})";
                using var doc = JsonDocument.Parse(json);
                return (doc.RootElement.GetProperty("choices").EnumerateArray().First().GetProperty("message").GetProperty("content").GetString() ?? "").Replace("**", "").Replace("#", "").Trim();
            }
            catch (Exception ex) { return $"Ошибка сети: {ex.Message}"; }
        }

        private Button Btn(Activity a, string t, string bg, string fg, float sz = 11, int px = 20, int py = 12) { var b = new Button(a) { Text = t, TextSize = sz }; b.SetPadding(px, py, px, py); b.SetMinimumWidth(0); b.SetMinimumHeight(0); var gd = new GradientDrawable(); gd.SetColor(Color.ParseColor(bg)); gd.SetCornerRadius(24f); b.Background = gd; b.SetTextColor(Color.ParseColor(fg)); return b; }
    }
}

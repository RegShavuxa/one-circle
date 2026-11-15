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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace one_circle
{
    public class AiHelper
    {
        private readonly List<(string Role, string Text)> history = new();
        private static readonly HttpClient http = new();

        public View BuildAiUI(Activity a, bool dark, Action onBack)
        {
            var prefs = a.GetSharedPreferences("oc_auth", FileCreationMode.Private);
            string nick = prefs?.GetString("nick", "Друг") ?? "Друг";
            string birth = prefs?.GetString("birth", "не указана") ?? "не указана";

            var root = new LinearLayout(a) { Orientation = Orientation.Vertical, LayoutParameters = new LinearLayout.LayoutParams(-1, -1) };
            root.SetBackgroundColor(Color.ParseColor(dark ? "#121216" : "#F5F7FA"));

            var top = new LinearLayout(a) { Orientation = Orientation.Horizontal };
            top.SetGravity(GravityFlags.CenterVertical); top.SetPadding(18, 14, 18, 14); top.SetBackgroundColor(Color.ParseColor(dark ? "#1E1E24" : "#E2E8F0"));
            var btnBack = Btn(a, "⬅️ Назад", "#DC2626", "#FFFFFF");
            var lbl = new TextView(a) { Text = "🤖 Семейный ИИ", TextSize = 15, LayoutParameters = new LinearLayout.LayoutParams(0, -2, 1f) };
            lbl.SetTypeface(null, TypefaceStyle.Bold); lbl.SetTextColor(Color.ParseColor(dark ? "#FFFFFF" : "#1E293B")); lbl.SetPadding(16, 0, 0, 0);
            var btnInfo = Btn(a, "ℹ️", "#475569", "#FFFFFF", 11, 18, 10);
            top.AddView(btnBack); top.AddView(lbl); top.AddView(btnInfo); root.AddView(top);

            var scroll = new ScrollView(a) { LayoutParameters = new LinearLayout.LayoutParams(-1, 0, 1f) };
            var list = new LinearLayout(a) { Orientation = Orientation.Vertical }; list.SetPadding(24, 20, 24, 20);
            scroll.AddView(list); root.AddView(scroll);
            int maxW = (int)((a.Resources?.DisplayMetrics?.WidthPixels ?? 800) * 0.82);

            void Render() => a.RunOnUiThread(() =>
            {
                list.RemoveAllViews();
                if (history.Count == 0) { var em = new TextView(a) { Text = $"Привет, {nick}! 📅 Сегодня {DateTime.Now:dd.MM.yyyy}\n\nВыберите быстрый запрос ниже или задайте свой вопрос нейросети.", TextSize = 14, Gravity = GravityFlags.Center }; em.SetTextColor(Color.ParseColor("#94A3B8")); em.SetPadding(20, 90, 20, 0); list.AddView(em); return; }
                foreach (var m in history)
                {
                    bool isUser = m.Role == "user";
                    var row = new LinearLayout(a) { Orientation = Orientation.Vertical }; row.SetGravity(isUser ? GravityFlags.End : GravityFlags.Start);
                    var lp = new LinearLayout.LayoutParams(-1, -2); lp.SetMargins(0, 0, 0, 14); row.LayoutParameters = lp;
                    var b = new TextView(a) { Text = isUser ? m.Text : $"ИИ-помощник:\n{m.Text}", TextSize = 15, LayoutParameters = new LinearLayout.LayoutParams(-2, -2) };
                    b.SetMaxWidth(maxW); b.SetPadding(26, 16, 26, 16); b.SetTextColor(Color.ParseColor(isUser || dark ? "#FFFFFF" : "#1E293B"));
                    var gd = new GradientDrawable(); gd.SetColor(Color.ParseColor(isUser ? "#2563EB" : (dark ? "#27272A" : "#FFFFFF"))); gd.SetCornerRadius(24f); b.Background = gd;
                    row.AddView(b); list.AddView(row);
                }
                scroll.Post(() => scroll.FullScroll(FocusSearchDirection.Down));
            });

            async Task SendPrompt(string q)
            {
                history.Add(("user", q)); history.Add(("assistant", "⏳ Анализирую данные...")); Render();
                string reply = await AskAi(q, nick, birth);
                history[history.Count - 1] = ("assistant", reply); Render();
            }

            Render();

            var hScroll = new HorizontalScrollView(a) { HorizontalScrollBarEnabled = false };
            var chips = new LinearLayout(a) { Orientation = Orientation.Horizontal }; chips.SetPadding(16, 8, 16, 8);
            var q1 = Btn(a, "📅 Праздники сегодня", dark ? "#334155" : "#E2E8F0", dark ? "#FFFFFF" : "#1E293B", 11, 18, 10);
            var q2 = Btn(a, "📰 Новости города и страны", dark ? "#334155" : "#E2E8F0", dark ? "#FFFFFF" : "#1E293B", 11, 18, 10);
            var q3 = Btn(a, "🎂 Мой профиль и напоминания", dark ? "#334155" : "#E2E8F0", dark ? "#FFFFFF" : "#1E293B", 11, 18, 10);
            var lp2 = new LinearLayout.LayoutParams(-2, -2); lp2.SetMargins(10, 0, 10, 0); q2.LayoutParameters = lp2;
            chips.AddView(q1); chips.AddView(q2); chips.AddView(q3); hScroll.AddView(chips); root.AddView(hScroll);

            q1.Click += async (s, e) => await SendPrompt("Какой сегодня день, какие праздники отмечают в России и мире? Напомни о важных традициях.");
            q2.Click += async (s, e) => await SendPrompt("Расскажи главные добрые и полезные новости: что важного происходит в городе Зима, Иркутской области и России?");
            q3.Click += async (s, e) => await SendPrompt($"Посмотри мою дату рождения ({birth}), посчитай сколько мне лет, сколько дней до дня рождения и дай семейный совет на сегодня.");

            var bar = new LinearLayout(a) { Orientation = Orientation.Horizontal }; bar.SetPadding(16, 10, 16, 16); bar.SetBackgroundColor(Color.ParseColor(dark ? "#1E1E24" : "#E2E8F0"));
            var inp = new EditText(a) { Hint = "Спросить нейросеть...", TextSize = 15 }; inp.SetPadding(26, 16, 26, 16); inp.SetTextColor(Color.ParseColor(dark ? "#FFFFFF" : "#1E293B")); inp.SetHintTextColor(Color.ParseColor("#94A3B8"));
            var gdI = new GradientDrawable(); gdI.SetColor(Color.ParseColor(dark ? "#27272A" : "#FFFFFF")); gdI.SetCornerRadius(24f); inp.Background = gdI;
            var lpI = new LinearLayout.LayoutParams(0, -2, 1f); lpI.SetMargins(0, 0, 12, 0); inp.LayoutParameters = lpI;
            var btnSend = Btn(a, "➤", "#7C3AED", "#FFFFFF", 16, 28, 14);
            bar.AddView(inp); bar.AddView(btnSend); root.AddView(bar);

            btnSend.Click += async (s, e) => { string q = inp.Text?.Trim() ?? ""; if (q == "") return; inp.Text = ""; await SendPrompt(q); };
            btnInfo.Click += (s, e) => new AlertDialog.Builder(a).SetTitle("Прозрачность ИИ в one's circle").SetMessage($"• Используется облачная языковая нейросеть (OpenAI-совместимый шлюз Pollinations AI).\n• Учитывает сегодняшнюю дату ({DateTime.Now:dd.MM.yyyy}) и дату рождения из профиля ({birth}) для расчёта праздников и напоминаний.\n• Координаты GPS и личная переписка круга в нейросеть не передаются.").SetPositiveButton("Понятно", (s2, e2) => { }).Show();
            btnBack.Click += (s, e) => onBack();
            return root;
        }

        private async Task<string> AskAi(string prompt, string nick, string birth)
        {
            try
            {
                string url = "https" + "://" + "text.pollinations" + ".ai/openai";
                string sys = $"Ты семейный ИИ-помощник в приложении one's circle. Сегодня: {DateTime.Now:dd.MM.yyyy} ({DateTime.Now:dddd}). Пользователь: {nick}, дата рождения: {birth}. Локация: г. Зима, Иркутская область, Россия. Отвечай структурированно, доброжелательно и строго на русском языке.";
                var payload = new { model = "openai", messages = new[] { new { role = "system", content = sys }, new { role = "user", content = prompt } } };
                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                using var res = await http.SendAsync(req);
                string json = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return $"Ошибка сервера ({res.StatusCode}). Попробуйте ещё раз.";
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("choices").EnumerateArray().First().GetProperty("message").GetProperty("content").GetString() ?? "Пустой ответ";
            }
            catch (Exception ex) { return $"Ошибка сети: {ex.Message}"; }
        }

        private Button Btn(Activity a, string t, string bg, string fg, float sz = 11, int px = 20, int py = 12) { var b = new Button(a) { Text = t, TextSize = sz }; b.SetPadding(px, py, px, py); b.SetMinimumWidth(0); b.SetMinimumHeight(0); var gd = new GradientDrawable(); gd.SetColor(Color.ParseColor(bg)); gd.SetCornerRadius(24f); b.Background = gd; b.SetTextColor(Color.ParseColor(fg)); return b; }
    }
}

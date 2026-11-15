using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace one_circle
{
    public class ChatHelper
    {
        public class ChatMsg
        {
            public string Id { get; set; } = "";
            public string OwnerPhone { get; set; } = ""; // Постоянный ID для привязки "свой/чужой"
            public string SenderName { get; set; } = ""; // Отображаемый никнейм или имя
            public string Text { get; set; } = "";
            public string Time { get; set; } = "";
        }

        private List<ChatMsg> msgs = new();
        private static readonly HttpClient http = new();
        private bool active;

        private void LoadCache(Context c) { try { var j = c.GetSharedPreferences("oc_chat", FileCreationMode.Private)?.GetString("cache_v2", ""); if (!string.IsNullOrEmpty(j)) msgs = JsonSerializer.Deserialize<List<ChatMsg>>(j) ?? new(); } catch { } }
        private void SaveCache(Context c) => c.GetSharedPreferences("oc_chat", FileCreationMode.Private)?.Edit()?.PutString("cache_v2", JsonSerializer.Serialize(msgs))?.Apply();

        public View BuildChatUI(Activity a, bool dark, Action onBack)
        {
            active = true; LoadCache(a);
            var prefs = a.GetSharedPreferences("oc_auth", FileCreationMode.Private);
            string myPhone = prefs?.GetString("phone", "me") ?? "me";
            string nick = prefs?.GetString("nick", "") ?? "";
            string full = prefs?.GetString("fullname", "") ?? "";
            string displayName = !string.IsNullOrEmpty(full) ? $"{nick} ({full})" : (!string.IsNullOrEmpty(nick) ? nick : "Я");
            string room = prefs?.GetString("room", "oc-family-2026") ?? "oc-family-2026";

            var root = new LinearLayout(a) { Orientation = Orientation.Vertical, LayoutParameters = new LinearLayout.LayoutParams(-1, -1) };
            root.SetBackgroundColor(Color.ParseColor(dark ? "#121216" : "#F5F7FA"));

            var top = new LinearLayout(a) { Orientation = Orientation.Horizontal };
            top.SetGravity(GravityFlags.CenterVertical); top.SetPadding(20, 14, 20, 14); top.SetBackgroundColor(Color.ParseColor(dark ? "#1E1E24" : "#E2E8F0"));
            var btnBack = Btn(a, "⬅️ Назад", "#DC2626", "#FFFFFF");
            var lbl = new TextView(a) { Text = $"💬 {room}", TextSize = 15, LayoutParameters = new LinearLayout.LayoutParams(0, -2, 1f) };
            lbl.SetTypeface(null, TypefaceStyle.Bold); lbl.SetTextColor(Color.ParseColor(dark ? "#FFFFFF" : "#1E293B")); lbl.SetPadding(20, 0, 0, 0);
            var btnCode = Btn(a, "🔗 Код", "#2563EB", "#FFFFFF", 12, 18, 10);
            top.AddView(btnBack); top.AddView(lbl); top.AddView(btnCode); root.AddView(top);

            var scroll = new ScrollView(a) { LayoutParameters = new LinearLayout.LayoutParams(-1, 0, 1f) };
            var list = new LinearLayout(a) { Orientation = Orientation.Vertical }; list.SetPadding(24, 20, 24, 20);
            scroll.AddView(list); root.AddView(scroll);
            int maxW = (int)((a.Resources?.DisplayMetrics?.WidthPixels ?? 800) * 0.75);

            void Render() => a.RunOnUiThread(() =>
            {
                list.RemoveAllViews();
                if (msgs.Count == 0) { var em = new TextView(a) { Text = $"Комната: {room}\nНажмите «🔗 Код» и введите его на втором телефоне.", TextSize = 14, Gravity = GravityFlags.Center }; em.SetTextColor(Color.ParseColor("#94A3B8")); em.SetPadding(20, 120, 20, 0); list.AddView(em); return; }
                foreach (var m in msgs)
                {
                    // Проверяем по номеру телефона, поэтому смена никнейма не ломает сторону сообщения
                    bool isMe = m.OwnerPhone == myPhone;
                    string shownName = isMe ? displayName : m.SenderName;

                    var row = new LinearLayout(a) { Orientation = Orientation.Vertical }; row.SetGravity(isMe ? GravityFlags.End : GravityFlags.Start);
                    var lp = new LinearLayout.LayoutParams(-1, -2); lp.SetMargins(0, 0, 0, 14); row.LayoutParameters = lp;

                    var b = new TextView(a) { Text = $"{shownName}\n{m.Text}  •  {m.Time}", TextSize = 15, LayoutParameters = new LinearLayout.LayoutParams(-2, -2) };
                    b.SetMaxWidth(maxW); b.SetPadding(28, 16, 28, 16); b.SetTextColor(Color.ParseColor(isMe || dark ? "#FFFFFF" : "#1E293B"));
                    var gd = new GradientDrawable(); gd.SetColor(Color.ParseColor(isMe ? "#2563EB" : (dark ? "#27272A" : "#FFFFFF"))); gd.SetCornerRadius(28f); b.Background = gd;
                    row.AddView(b); list.AddView(row);
                }
                scroll.Post(() => scroll.FullScroll(FocusSearchDirection.Down));
            });

            string TopicUrl() => "https" + "://" + "ntfy" + $".sh/{room}/json?poll=1&since=12h";
            string PostUrl() => "https" + "://" + "ntfy" + $".sh/{room}";

            async Task Sync()
            {
                try
                {
                    string raw = await http.GetStringAsync(TopicUrl());
                    bool added = false;
                    foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        using var doc = JsonDocument.Parse(line);
                        if (doc.RootElement.TryGetProperty("message", out var msgProp))
                        {
                            var cm = JsonSerializer.Deserialize<ChatMsg>(msgProp.GetString() ?? "");
                            if (cm != null && !string.IsNullOrEmpty(cm.Id) && !msgs.Exists(x => x.Id == cm.Id)) { msgs.Add(cm); added = true; }
                        }
                    }
                    if (added) { SaveCache(a); Render(); }
                }
                catch { }
            }

            Render();
            _ = Task.Run(async () => { while (active) { await Sync(); await Task.Delay(2500); } });

            var bar = new LinearLayout(a) { Orientation = Orientation.Horizontal }; bar.SetPadding(16, 12, 16, 16); bar.SetBackgroundColor(Color.ParseColor(dark ? "#1E1E24" : "#E2E8F0"));
            var inp = new EditText(a) { Hint = "Сообщение...", TextSize = 15 }; inp.SetPadding(26, 16, 26, 16); inp.SetTextColor(Color.ParseColor(dark ? "#FFFFFF" : "#1E293B")); inp.SetHintTextColor(Color.ParseColor("#94A3B8"));
            var gdI = new GradientDrawable(); gdI.SetColor(Color.ParseColor(dark ? "#27272A" : "#FFFFFF")); gdI.SetCornerRadius(28f); inp.Background = gdI;
            var lpI = new LinearLayout.LayoutParams(0, -2, 1f); lpI.SetMargins(0, 0, 12, 0); inp.LayoutParameters = lpI;
            var btnSend = Btn(a, "➤", "#16A34A", "#FFFFFF", 16, 28, 14);
            bar.AddView(inp); bar.AddView(btnSend); root.AddView(bar);

            btnSend.Click += async (s, e) =>
            {
                string t = inp.Text?.Trim() ?? ""; if (t == "") return;
                var m = new ChatMsg { Id = Guid.NewGuid().ToString("N"), OwnerPhone = myPhone, SenderName = displayName, Text = t, Time = DateTime.Now.ToString("HH:mm") };
                msgs.Add(m); SaveCache(a); inp.Text = ""; Render();
                try { await http.PostAsync(PostUrl(), new StringContent(JsonSerializer.Serialize(m), Encoding.UTF8, "text/plain")); } catch { }
            };

            btnCode.Click += (s, e) =>
            {
                var ed = new EditText(a) { Text = room, Hint = "Придумайте общий код" };
                new AlertDialog.Builder(a).SetTitle("Подключение второго человека").SetMessage("Введите одинаковый код на обоих телефонах:").SetView(ed)
                    .SetPositiveButton("Подключить", async (s2, e2) => { string nc = ed.Text?.Trim().ToLower() ?? ""; if (nc == "") return; room = nc; lbl.Text = $"💬 {room}"; prefs?.Edit()?.PutString("room", room)?.Apply(); msgs.Clear(); SaveCache(a); Render(); await Sync(); }).Show();
            };

            btnBack.Click += (s, e) => { active = false; onBack(); };
            return root;
        }

        private Button Btn(Activity a, string t, string bg, string fg, float sz = 11, int px = 20, int py = 12) { var b = new Button(a) { Text = t, TextSize = sz }; b.SetPadding(px, py, px, py); b.SetMinimumWidth(0); b.SetMinimumHeight(0); var gd = new GradientDrawable(); gd.SetColor(Color.ParseColor(bg)); gd.SetCornerRadius(24f); b.Background = gd; b.SetTextColor(Color.ParseColor(fg)); return b; }
    }
}

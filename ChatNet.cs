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
    public class ChatMsg { public string Type { get; set; } = "msg"; public string Id { get; set; } = ""; public string OwnerPhone { get; set; } = ""; public string SenderName { get; set; } = ""; public string Text { get; set; } = ""; public string Time { get; set; } = ""; }

    public static class ChatNet
    {
        private static readonly HttpClient http = new();

        public static void Load(Context c, string room, out List<ChatMsg> m, out HashSet<string> h, out HashSet<string> d)
        {
            m = new(); h = new(); d = new();
            try { var p = c.GetSharedPreferences("oc_chat", FileCreationMode.Private); m = JsonSerializer.Deserialize<List<ChatMsg>>(p?.GetString($"c_{room}", "") ?? "") ?? new(); h = JsonSerializer.Deserialize<HashSet<string>>(p?.GetString($"h_{room}", "") ?? "") ?? new(); d = JsonSerializer.Deserialize<HashSet<string>>(p?.GetString($"d_{room}", "") ?? "") ?? new(); } catch { }
        }

        public static void Save(Context c, string room, List<ChatMsg> m, HashSet<string> h, HashSet<string> d) =>
            c.GetSharedPreferences("oc_chat", FileCreationMode.Private)?.Edit()?.PutString($"c_{room}", JsonSerializer.Serialize(m))?.PutString($"h_{room}", JsonSerializer.Serialize(h))?.PutString($"d_{room}", JsonSerializer.Serialize(d))?.Apply();

        public static async Task Send(string room, string key, ChatMsg msg)
        {
            try { await http.PostAsync("https" + "://" + "ntfy" + $".sh/{room}", new StringContent(ChatSecurity.Encrypt(JsonSerializer.Serialize(msg), key), Encoding.UTF8, "text/plain")); } catch { }
        }

        public static async Task<bool> Poll(string room, string key, List<ChatMsg> msgs, HashSet<string> del)
        {
            try
            {
                string raw = await http.GetStringAsync("https" + "://" + "ntfy" + $".sh/{room}/json?poll=1&since=12h"); bool ch = false;
                foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    using var doc = JsonDocument.Parse(line); if (!doc.RootElement.TryGetProperty("message", out var mp)) continue;
                    string dec = ChatSecurity.Decrypt(mp.GetString() ?? "", key); var cm = string.IsNullOrEmpty(dec) ? null : JsonSerializer.Deserialize<ChatMsg>(dec);
                    if (cm == null || string.IsNullOrEmpty(cm.Id)) continue;
                    if (cm.Type == "del") { if (del.Add(cm.Id)) ch = true; if (msgs.RemoveAll(x => x.Id == cm.Id) > 0) ch = true; }
                    else if (!del.Contains(cm.Id) && !msgs.Exists(x => x.Id == cm.Id)) { msgs.Add(cm); ch = true; }
                }
                return ch;
            }
            catch { return false; }
        }

        public static Button Btn(Activity a, string t, string bg, string fg, float sz = 11, int px = 20, int py = 12)
        {
            var b = new Button(a) { Text = t, TextSize = sz }; b.SetPadding(px, py, px, py); b.SetMinimumWidth(0); b.SetMinimumHeight(0);
            var gd = new GradientDrawable(); gd.SetColor(Color.ParseColor(bg)); gd.SetCornerRadius(24f); b.Background = gd; b.SetTextColor(Color.ParseColor(fg)); return b;
        }

        public static LinearLayout MakeBar(Activity a, bool dark, Action<string> onSend)
        {
            var bar = new LinearLayout(a) { Orientation = Orientation.Horizontal }; bar.SetPadding(16, 12, 16, 16); bar.SetBackgroundColor(Color.ParseColor(dark ? "#1E1E24" : "#E2E8F0"));
            var inp = new EditText(a) { Hint = "Сообщение...", TextSize = 15 }; inp.SetPadding(26, 16, 26, 16); inp.SetTextColor(Color.ParseColor(dark ? "#FFFFFF" : "#1E293B")); inp.SetHintTextColor(Color.ParseColor("#94A3B8"));
            var gd = new GradientDrawable(); gd.SetColor(Color.ParseColor(dark ? "#27272A" : "#FFFFFF")); gd.SetCornerRadius(28f); inp.Background = gd;
            var lp = new LinearLayout.LayoutParams(0, -2, 1f); lp.SetMargins(0, 0, 12, 0); inp.LayoutParameters = lp;
            var btn = Btn(a, "➤", "#16A34A", "#FFFFFF", 16, 28, 14); bar.AddView(inp); bar.AddView(btn);
            btn.Click += (s, e) => { string t = inp.Text?.Trim() ?? ""; if (t != "") { inp.Text = ""; onSend(t); } };
            return bar;
        }
    }
}

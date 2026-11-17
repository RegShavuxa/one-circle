using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace one_circle
{
    public class ChatHelper
    {
        private List<ChatMsg> msgs = new(); private HashSet<string> hid = new(), del = new(); private bool active;

        public View BuildChatUI(Activity a, bool dark, Action onBack)
        {
            var host = new LinearLayout(a) { Orientation = Orientation.Vertical, LayoutParameters = new LinearLayout.LayoutParams(-1, -1) };
            void ShowList() { active = false; host.RemoveAllViews(); host.AddView(BuildList(a, dark, onBack, item => { host.RemoveAllViews(); host.AddView(BuildRoom(a, dark, item, ShowList)); })); }
            ShowList(); return host;
        }

        private View BuildList(Activity a, bool dark, Action onBack, Action<ChatRoomItem> onOpen)
        {
            var root = new LinearLayout(a) { Orientation = Orientation.Vertical, LayoutParameters = new LinearLayout.LayoutParams(-1, -1) }; root.SetBackgroundColor(Color.ParseColor(dark ? "#121216" : "#F5F7FA"));
            var top = new LinearLayout(a) { Orientation = Orientation.Horizontal }; top.SetGravity(GravityFlags.CenterVertical); top.SetPadding(20, 14, 20, 14); top.SetBackgroundColor(Color.ParseColor(dark ? "#1E1E24" : "#E2E8F0"));
            var btnBack = ChatNet.Btn(a, "⬅️ Назад", "#DC2626", "#FFFFFF");
            var lbl = new TextView(a) { Text = "💬 Мои чаты", TextSize = 16, LayoutParameters = new LinearLayout.LayoutParams(0, -2, 1f) }; lbl.SetTypeface(null, TypefaceStyle.Bold); lbl.SetTextColor(Color.ParseColor(dark ? "#FFFFFF" : "#1E293B")); lbl.SetPadding(20, 0, 0, 0);
            var btnNew = ChatNet.Btn(a, "➕ Чат", "#2563EB", "#FFFFFF", 12, 18, 10);
            top.AddView(btnBack); top.AddView(lbl); top.AddView(btnNew); root.AddView(top);

            var scroll = new ScrollView(a) { LayoutParameters = new LinearLayout.LayoutParams(-1, 0, 1f) };
            var list = new LinearLayout(a) { Orientation = Orientation.Vertical }; list.SetPadding(24, 20, 24, 20); scroll.AddView(list); root.AddView(scroll);
            void Draw() { list.RemoveAllViews(); foreach (var c in ChatSecurity.GetAllChats(a)) { var it = c; var b = ChatNet.Btn(a, $"{it.Title}\nКанал: {it.Room}", dark ? "#1E1E24" : "#FFFFFF", dark ? "#FFFFFF" : "#1E293B", 14, 28, 22); b.Gravity = GravityFlags.Start; var lp = new LinearLayout.LayoutParams(-1, -2); lp.SetMargins(0, 0, 0, 14); b.LayoutParameters = lp; b.Click += (s, e) => onOpen(it); list.AddView(b); } }
            Draw(); _ = Task.Run(async () => { await CircleSync.PullMembers(a); a.RunOnUiThread(Draw); });

            btnNew.Click += (s, e) => MainActivity.ShowThemedDialog(a, "Новый чат или ссылка", (box, dlg) => {
                bool sd = MainActivity.IsSystemDark(a);
                var ed = new EditText(a) { Hint = "Название или ссылка...", TextSize = 14 }; ed.SetPadding(24, 16, 24, 16); ed.SetTextColor(Color.ParseColor(sd ? "#FFFFFF" : "#1E293B")); ed.SetHintTextColor(Color.ParseColor("#94A3B8"));
                var gdE = new GradientDrawable(); gdE.SetColor(Color.ParseColor(sd ? "#27272A" : "#F1F5F9")); gdE.SetCornerRadius(20f); ed.Background = gdE; box.AddView(ed);
                void Opt(string t, string bg, string fg, Action act) { var b = ChatNet.Btn(a, t, bg, fg, 14, 24, 16); var l2 = new LinearLayout.LayoutParams(-1, -2); l2.SetMargins(0, 14, 0, 0); b.LayoutParameters = l2; b.Click += (s2, e2) => { dlg?.Dismiss(); act(); }; box.AddView(b); }
                Opt("Создать чат", "#2563EB", "#FFFFFF", () => { string nm = string.IsNullOrWhiteSpace(ed.Text) ? "Комната" : ed.Text.Trim(); ChatSecurity.SaveExtraChat(a, $"💬 {nm}", $"oc{Guid.NewGuid().ToString("N").Substring(0, 6)}", Guid.NewGuid().ToString("N").Substring(0, 12)); Draw(); });
                Opt("Войти по ссылке", "#16A34A", "#FFFFFF", () => { if (!ChatSecurity.TryJoinByUrl(a, ed.Text ?? "", out _, out _)) { Toast.MakeText(a, "Неверная ссылка", ToastLength.Short)?.Show(); return; } Draw(); });
            });
            btnBack.Click += (s, e) => onBack(); return root;
        }
        private View BuildRoom(Activity a, bool dark, ChatRoomItem ri, Action onBackToList)
        {
            active = true; string room = ri.Room, rKey = ri.Key; ChatNet.Load(a, room, out msgs, out hid, out del);
            var p = a.GetSharedPreferences("oc_auth", FileCreationMode.Private); string myPh = p?.GetString("phone", "me") ?? "me", nk = p?.GetString("nick", "") ?? "", fn = p?.GetString("fullname", "") ?? "";
            string disp = !string.IsNullOrEmpty(fn) ? $"{nk} ({fn})" : (!string.IsNullOrEmpty(nk) ? nk : "Я");

            var root = new LinearLayout(a) { Orientation = Orientation.Vertical, LayoutParameters = new LinearLayout.LayoutParams(-1, -1) }; root.SetBackgroundColor(Color.ParseColor(dark ? "#121216" : "#F5F7FA"));
            var top = new LinearLayout(a) { Orientation = Orientation.Horizontal }; top.SetGravity(GravityFlags.CenterVertical); top.SetPadding(20, 14, 20, 14); top.SetBackgroundColor(Color.ParseColor(dark ? "#1E1E24" : "#E2E8F0"));
            var btnBack = ChatNet.Btn(a, "⬅️ Чаты", "#DC2626", "#FFFFFF"); var lbl = new TextView(a) { Text = ri.Title, TextSize = 14, LayoutParameters = new LinearLayout.LayoutParams(0, -2, 1f) }; lbl.SetTypeface(null, TypefaceStyle.Bold); lbl.SetTextColor(Color.ParseColor(dark ? "#FFFFFF" : "#1E293B")); lbl.SetPadding(16, 0, 8, 0);
            var btnLink = ChatNet.Btn(a, "🔗 Ссылка", "#2563EB", "#FFFFFF", 12, 16, 10); top.AddView(btnBack); top.AddView(lbl); top.AddView(btnLink); root.AddView(top);

            var scroll = new ScrollView(a) { LayoutParameters = new LinearLayout.LayoutParams(-1, 0, 1f) }; var list = new LinearLayout(a) { Orientation = Orientation.Vertical }; list.SetPadding(24, 20, 24, 20); scroll.AddView(list); root.AddView(scroll);
            int maxW = (int)((a.Resources?.DisplayMetrics?.WidthPixels ?? 800) * 0.75);

            void Render() => a.RunOnUiThread(() => {
                list.RemoveAllViews(); var vis = msgs.FindAll(m => !hid.Contains(m.Id) && !del.Contains(m.Id));
                if (vis.Count == 0) { var em = new TextView(a) { Text = $"{ri.Title}\nСообщений пока нет.", TextSize = 14, Gravity = GravityFlags.Center }; em.SetTextColor(Color.ParseColor("#94A3B8")); em.SetPadding(20, 120, 20, 0); list.AddView(em); return; }
                foreach (var m in vis)
                {
                    bool isMe = m.OwnerPhone == myPh; var row = new LinearLayout(a) { Orientation = Orientation.Vertical }; row.SetGravity(isMe ? GravityFlags.End : GravityFlags.Start); var lp = new LinearLayout.LayoutParams(-1, -2); lp.SetMargins(0, 0, 0, 14); row.LayoutParameters = lp;
                    var bub = new LinearLayout(a) { Orientation = Orientation.Vertical, LayoutParameters = new LinearLayout.LayoutParams(-2, -2) }; bub.SetPadding(28, 14, 28, 16); var gd = new GradientDrawable(); gd.SetColor(Color.ParseColor(isMe ? "#2563EB" : (dark ? "#27272A" : "#FFFFFF"))); gd.SetCornerRadius(28f); bub.Background = gd;
                    var tvN = new TextView(a) { Text = isMe ? disp : m.SenderName, TextSize = 12, Gravity = isMe ? GravityFlags.End : GravityFlags.Start, LayoutParameters = new LinearLayout.LayoutParams(-1, -2) }; tvN.SetMaxWidth(maxW); tvN.SetTypeface(null, TypefaceStyle.Bold); tvN.SetPadding(0, 0, 0, 4); tvN.SetTextColor(Color.ParseColor(isMe ? "#BAE6FD" : (dark ? "#38BDF8" : "#2563EB")));
                    var tvT = new TextView(a) { Text = $"{m.Text}  •  {m.Time}", TextSize = 15, Gravity = GravityFlags.Start, LayoutParameters = new LinearLayout.LayoutParams(-2, -2) }; tvT.SetMaxWidth(maxW); tvT.SetTextColor(Color.ParseColor(isMe || dark ? "#FFFFFF" : "#1E293B"));
                    bub.Click += (s, e) => MainActivity.ShowThemedDialog(a, "Удалить сообщение", (box, dlg) => {
                        bool sd = MainActivity.IsSystemDark(a); void Opt(string t, string bg, string fg, Action act) { var b = ChatNet.Btn(a, t, bg, fg, 14, 24, 18); var l2 = new LinearLayout.LayoutParams(-1, -2); l2.SetMargins(0, 0, 0, 14); b.LayoutParameters = l2; b.Click += (s2, e2) => { dlg?.Dismiss(); act(); }; box.AddView(b); }
                        if (isMe) Opt("Удалить у всех", "#DC2626", "#FFFFFF", async () => { del.Add(m.Id); msgs.RemoveAll(x => x.Id == m.Id); ChatNet.Save(a, room, msgs, hid, del); Render(); await ChatNet.Send(room, rKey, new ChatMsg { Type = "del", Id = m.Id, OwnerPhone = myPh }); });
                        Opt("Удалить только у меня", sd ? "#334155" : "#E2E8F0", sd ? "#FFFFFF" : "#1E293B", () => { hid.Add(m.Id); ChatNet.Save(a, room, msgs, hid, del); Render(); });
                        Opt("Отмена", sd ? "#27272A" : "#F1F5F9", sd ? "#94A3B8" : "#475569", () => { });
                    });
                    bub.AddView(tvN); bub.AddView(tvT); row.AddView(bub); list.AddView(row);
                }
                scroll.Post(() => scroll.FullScroll(FocusSearchDirection.Down));
            });

            async Task Sync() { if (await ChatNet.Poll(room, rKey, msgs, del)) { ChatNet.Save(a, room, msgs, hid, del); Render(); } }
            Render(); _ = Task.Run(async () => { while (active) { await Sync(); await Task.Delay(2500); } });

            root.AddView(ChatNet.MakeBar(a, dark, async t => { var m = new ChatMsg { Type = "msg", Id = Guid.NewGuid().ToString("N"), OwnerPhone = myPh, SenderName = disp, Text = t, Time = DateTime.Now.ToString("HH:mm") }; msgs.Add(m); ChatNet.Save(a, room, msgs, hid, del); Render(); await ChatNet.Send(room, rKey, m); }));
            btnLink.Click += (s, e) => { string url = ChatSecurity.MakeInviteUrl(room, rKey); var i = new Intent(Intent.ActionSend); i.SetType("text/plain"); i.PutExtra(Intent.ExtraText, $"Чат «{ri.Title}» в one's circle:\n{url}"); a.StartActivity(Intent.CreateChooser(i, "Поделиться")); };
            btnBack.Click += (s, e) => { active = false; onBackToList(); };
            return root;
        }
    }
}

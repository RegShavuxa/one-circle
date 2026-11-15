using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using System;

namespace one_circle
{
    public class CircleHelper
    {
        public bool IsMapOpened { get; private set; }
        private bool isDark, autoFollow = true, hasLoc;
        private double lat, lon;
        private WebView? web;
        private Button? btnGps, btnCircle, btnTheme;
        private LinearLayout? root, bar, welcome, mapBox, subBox;
        private TextView? title, txtW;
        private readonly MapDialogHelper mapDlg = new();
        private readonly ChatHelper chat = new();
        private readonly AuthHelper auth = new();
        private readonly AiHelper ai = new();

        public View BuildUI(Activity a, Action onGps)
        {
            isDark = ((a.Resources?.Configuration?.UiMode ?? Android.Content.Res.UiMode.NightUndefined) & Android.Content.Res.UiMode.NightMask) == Android.Content.Res.UiMode.NightYes;
            root = new LinearLayout(a) { Orientation = Orientation.Vertical }; root.SetFitsSystemWindows(true);

            welcome = new LinearLayout(a) { Orientation = Orientation.Vertical, LayoutParameters = new LinearLayout.LayoutParams(-1, -1) };
            bar = new LinearLayout(a) { Orientation = Orientation.Horizontal }; bar.SetGravity(GravityFlags.CenterVertical); bar.SetPadding(18, 56, 18, 12);
            btnTheme = Btn(a, "🌙 Тёмная", "#FFFFFF", "#1E293B", 9.5f, 12, 7);
            title = new TextView(a) { Text = "one's circle", TextSize = 16, LayoutParameters = new LinearLayout.LayoutParams(0, -2, 1f) }; title.SetTypeface(null, TypefaceStyle.Bold); title.SetPadding(14, 0, 8, 0);
            var btnInv = Btn(a, "➕ Пригласить", "#2563EB", "#FFFFFF", 11f, 14, 6);
            var btnProf = Btn(a, "👤 Профиль", "#475569", "#FFFFFF", 9.5f, 12, 7);
            var lpP = new LinearLayout.LayoutParams(-2, -2); lpP.SetMargins(8, 0, 0, 0); btnProf.LayoutParameters = lpP;
            bar.AddView(btnTheme); bar.AddView(title); bar.AddView(btnInv); bar.AddView(btnProf); welcome.AddView(bar);

            var center = new LinearLayout(a) { Orientation = Orientation.Vertical, LayoutParameters = new LinearLayout.LayoutParams(-1, 0, 1f) }; center.SetGravity(GravityFlags.Center);
            txtW = new TextView(a) { Text = "Добро пожаловать в one's circle\n\nВыберите раздел ниже:", TextSize = 16, Gravity = GravityFlags.Center }; txtW.SetPadding(40, 0, 40, 36);
            var btnOpen = Btn(a, "🗺️ Открыть карту круга", "#2563EB", "#FFFFFF", 15, 45, 22);

            // Две кнопки ровно по половине ширины экрана каждая (вес 1f и ширина 0)
            var rowBtns = new LinearLayout(a) { Orientation = Orientation.Horizontal, LayoutParameters = new LinearLayout.LayoutParams(-1, -2) }; rowBtns.SetPadding(24, 22, 24, 0);
            var btnChat = Btn(a, "💬 Чат круга", "#16A34A", "#FFFFFF", 13, 10, 20); SetW(btnChat, 0, 8);
            var btnAi = Btn(a, "🤖 ИИ-помощник", "#7C3AED", "#FFFFFF", 13, 10, 20); SetW(btnAi, 8, 0);
            rowBtns.AddView(btnChat); rowBtns.AddView(btnAi);

            center.AddView(txtW); center.AddView(btnOpen); center.AddView(rowBtns); welcome.AddView(center); root.AddView(welcome);

            mapBox = new LinearLayout(a) { Orientation = Orientation.Vertical, Visibility = ViewStates.Gone, LayoutParameters = new LinearLayout.LayoutParams(-1, -1) };
            var actBar = new LinearLayout(a) { Orientation = Orientation.Horizontal }; actBar.SetPadding(16, 48, 16, 12);
            var btnExit = Btn(a, "⬅️ Выйти", "#DC2626", "#FFFFFF", 11, 24, 14);
            btnGps = Btn(a, "📡 Следит", "#16A34A", "#FFFFFF", 11, 20, 14); SetW(btnGps, 12, 12);
            btnCircle = Btn(a, "👥 Круг", "#E2E8F0", "#1E293B", 11, 20, 14); SetW(btnCircle, 0, 0);
            actBar.AddView(btnExit); actBar.AddView(btnGps); actBar.AddView(btnCircle); mapBox.AddView(actBar);
            web = new WebView(a) { LayoutParameters = new LinearLayout.LayoutParams(-1, -1) }; web.Settings.JavaScriptEnabled = true; web.Settings.DomStorageEnabled = true; web.SetWebViewClient(new WebViewClient());
            mapBox.AddView(web); root.AddView(mapBox);

            subBox = new LinearLayout(a) { Orientation = Orientation.Vertical, Visibility = ViewStates.Gone, LayoutParameters = new LinearLayout.LayoutParams(-1, -1) }; subBox.SetPadding(0, 36, 0, 0); root.AddView(subBox);
            ApplyTheme();

            btnOpen.Click += (s, e) => { SetScr(true, false); if (hasLoc) mapDlg.ShowMap(web, lat, lon, 0.002); onGps(); };
            btnChat.Click += (s, e) => { subBox.RemoveAllViews(); subBox.AddView(chat.BuildChatUI(a, isDark, () => SetScr(false, false))); SetScr(false, true); };
            btnAi.Click += (s, e) => { subBox.RemoveAllViews(); subBox.AddView(ai.BuildAiUI(a, isDark, () => SetScr(false, false))); SetScr(false, true); };
            btnProf.Click += (s, e) => { subBox.RemoveAllViews(); subBox.AddView(auth.BuildProfileUI(a, isDark, () => SetScr(false, false))); SetScr(false, true); };
            btnExit.Click += (s, e) => { SetScr(false, false); autoFollow = true; UpdGps(); web?.StopLoading(); web?.LoadUrl("about:blank"); web?.ClearCache(true); };
            btnGps.Click += (s, e) => { autoFollow = !autoFollow; UpdGps(); if (autoFollow) { if (hasLoc) mapDlg.ShowMap(web, lat, lon, 0.002); onGps(); } };
            btnInv.Click += (s, e) => mapDlg.ShowInvite(a, lat, lon, hasLoc, (nLa, nLo) => { SetScr(true, false); onGps(); autoFollow = false; UpdGps(); mapDlg.ShowMap(web, nLa, nLo, 0.003); });
            btnCircle.Click += (s, e) => mapDlg.ShowMembers(a, lat, lon, hasLoc, () => { SetScr(true, false); onGps(); autoFollow = true; UpdGps(); mapDlg.ShowMap(web, lat, lon, 0.002); }, (mLa, mLo) => { SetScr(true, false); onGps(); autoFollow = false; UpdGps(); mapDlg.ShowMap(web, mLa, mLo, 0.002); });
            btnTheme.Click += (s, e) => { isDark = !isDark; ApplyTheme(); };
            return root;
        }

        private void ApplyTheme() { root?.SetBackgroundColor(Color.ParseColor(isDark ? "#121216" : "#F5F7FA")); bar?.SetBackgroundColor(Color.ParseColor(isDark ? "#1E1E24" : "#E2E8F0")); title?.SetTextColor(isDark ? Color.White : Color.ParseColor("#1E293B")); txtW?.SetTextColor(isDark ? Color.LightGray : Color.ParseColor("#475569")); if (btnTheme != null) { btnTheme.Text = isDark ? "☀️ Светлая" : "🌙 Тёмная"; Style(btnTheme, isDark ? "#334155" : "#FFFFFF", isDark ? "#FFFFFF" : "#1E293B"); } Style(btnCircle, isDark ? "#334155" : "#E2E8F0", isDark ? "#FFFFFF" : "#1E293B"); }
        private Button Btn(Activity a, string t, string bg, string fg, float sz = 12, int px = 22, int py = 12) { var b = new Button(a) { Text = t, TextSize = sz }; b.SetPadding(px, py, px, py); b.SetMinimumWidth(0); b.SetMinimumHeight(0); Style(b, bg, fg); return b; }
        private void Style(Button? b, string bg, string fg) { if (b == null) return; var gd = new GradientDrawable(); gd.SetColor(Color.ParseColor(bg)); gd.SetCornerRadius(20f); b.Background = gd; b.SetTextColor(Color.ParseColor(fg)); }
        private void SetW(Button b, int l, int r) { var lp = new LinearLayout.LayoutParams(0, -2, 1f); lp.SetMargins(l, 0, r, 0); b.LayoutParameters = lp; }
        private void SetScr(bool m, bool s) { IsMapOpened = m; if (welcome != null) welcome.Visibility = (!m && !s) ? ViewStates.Visible : ViewStates.Gone; if (mapBox != null) mapBox.Visibility = m ? ViewStates.Visible : ViewStates.Gone; if (subBox != null) subBox.Visibility = s ? ViewStates.Visible : ViewStates.Gone; }
        private void UpdGps() { if (btnGps == null) return; btnGps.Text = autoFollow ? "📡 Следит" : "⏸️ Пауза"; Style(btnGps, autoFollow ? "#16A34A" : "#64748B", "#FFFFFF"); }
        public void UpdateLocation(double nLa, double nLo, bool f) { bool mv = Math.Abs(lat - nLa) > 0.00001 || Math.Abs(lon - nLo) > 0.00001; lat = nLa; lon = nLo; hasLoc = true; if (IsMapOpened && autoFollow && (f || mv)) mapDlg.ShowMap(web, lat, lon, 0.002); }
    }
}

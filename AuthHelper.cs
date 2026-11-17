using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Text;
using Android.Views;
using Android.Widget;
using System;

namespace one_circle
{
    public class AuthHelper
    {
        public bool IsLoggedIn(Context c) => !string.IsNullOrEmpty(c.GetSharedPreferences("oc_auth", FileCreationMode.Private)?.GetString("phone", ""));

        public View BuildAuthUI(Activity a, Action onSuccess)
        {
            var night = (a.Resources?.Configuration?.UiMode ?? Android.Content.Res.UiMode.NightUndefined) & Android.Content.Res.UiMode.NightMask;
            bool isDark = night == Android.Content.Res.UiMode.NightYes;

            var root = new LinearLayout(a) { Orientation = Orientation.Vertical };
            root.SetBackgroundColor(Color.ParseColor(isDark ? "#121216" : "#F5F7FA"));
            root.SetGravity(GravityFlags.Center); root.SetPadding(50, 40, 50, 40);

            var title = new TextView(a) { Text = "one's circle", TextSize = 26, Gravity = GravityFlags.Center };
            title.SetTypeface(null, TypefaceStyle.Bold); title.SetTextColor(Color.ParseColor(isDark ? "#FFFFFF" : "#1E293B")); title.SetPadding(0, 0, 0, 10);

            var sub = new TextView(a) { Text = "Создание профиля в семейном круге", TextSize = 14, Gravity = GravityFlags.Center };
            sub.SetTextColor(Color.ParseColor(isDark ? "#94A3B8" : "#475569")); sub.SetPadding(0, 0, 0, 32);

            var inpPhone = MakeInput(a, "Номер телефона (+7...)", InputTypes.ClassPhone, isDark);
            var inpNick = MakeInput(a, "Логин (обязательно)", InputTypes.ClassText, isDark);
            var inpName = MakeInput(a, "Имя и фамилия (не обязательно)", InputTypes.ClassText | InputTypes.TextVariationPersonName, isDark);
            var inpBirth = MakeInput(a, "Дата рождения (ДД.ММ.ГГГГ)", InputTypes.ClassDatetime | InputTypes.DatetimeVariationDate, isDark);

            var btnLogin = Btn(a, "Войти в круг", "#2563EB", "#FFFFFF", 15, 40, 24);
            var lp = new LinearLayout.LayoutParams(-1, -2); lp.SetMargins(0, 20, 0, 0); btnLogin.LayoutParameters = lp;

            root.AddView(title); root.AddView(sub); root.AddView(inpPhone); root.AddView(inpNick); root.AddView(inpName); root.AddView(inpBirth); root.AddView(btnLogin);

            btnLogin.Click += (s, e) =>
            {
                string ph = inpPhone.Text?.Trim() ?? "", nk = inpNick.Text?.Trim() ?? "", fn = inpName.Text?.Trim() ?? "", bd = inpBirth.Text?.Trim() ?? "";
                if (ph.Length < 10) { Toast.MakeText(a, "Введите корректный номер телефона", ToastLength.Short)?.Show(); return; }
                if (nk.Length < 2) { Toast.MakeText(a, "Введите логин (минимум 2 символа)", ToastLength.Short)?.Show(); return; }
                if (bd.Length < 8) { Toast.MakeText(a, "Укажите дату рождения (ДД.ММ.ГГГГ)", ToastLength.Short)?.Show(); return; }

                a.GetSharedPreferences("oc_auth", FileCreationMode.Private)?.Edit()?.PutString("phone", ph)?.PutString("nick", nk)?.PutString("fullname", fn)?.PutString("birth", bd)?.Apply();
                Toast.MakeText(a, $"Добро пожаловать, {nk}!", ToastLength.Short)?.Show();
                onSuccess();
            };
            return root;
        }

        // Экран просмотра и редактирования профиля
        public View BuildProfileUI(Activity a, bool isDark, Action onBack)
        {
            var prefs = a.GetSharedPreferences("oc_auth", FileCreationMode.Private);
            var root = new LinearLayout(a) { Orientation = Orientation.Vertical, LayoutParameters = new LinearLayout.LayoutParams(-1, -1) };
            root.SetBackgroundColor(Color.ParseColor(isDark ? "#121216" : "#F5F7FA"));

            var top = new LinearLayout(a) { Orientation = Orientation.Horizontal };
            top.SetGravity(GravityFlags.CenterVertical); top.SetPadding(20, 14, 20, 14); top.SetBackgroundColor(Color.ParseColor(isDark ? "#1E1E24" : "#E2E8F0"));
            var btnBack = Btn(a, "⬅️ Назад", "#DC2626", "#FFFFFF", 11, 20, 12);
            var lbl = new TextView(a) { Text = "👤 Мой профиль", TextSize = 16, LayoutParameters = new LinearLayout.LayoutParams(0, -2, 1f) };
            lbl.SetTypeface(null, TypefaceStyle.Bold); lbl.SetTextColor(Color.ParseColor(isDark ? "#FFFFFF" : "#1E293B")); lbl.SetPadding(20, 0, 0, 0);
            top.AddView(btnBack); top.AddView(lbl); root.AddView(top);

            var box = new LinearLayout(a) { Orientation = Orientation.Vertical }; box.SetPadding(45, 36, 45, 36);
            var inpNick = MakeInput(a, "Логин", InputTypes.ClassText, isDark); inpNick.Text = prefs?.GetString("nick", "");
            var inpName = MakeInput(a, "Имя и фамилия (не обязательно)", InputTypes.ClassText, isDark); inpName.Text = prefs?.GetString("fullname", "");
            var inpBirth = MakeInput(a, "Дата рождения", InputTypes.ClassDatetime, isDark); inpBirth.Text = prefs?.GetString("birth", "");
            var inpPhone = MakeInput(a, "Номер телефона", InputTypes.ClassPhone, isDark); inpPhone.Text = prefs?.GetString("phone", "");

            var btnSave = Btn(a, "💾 Сохранить изменения", "#16A34A", "#FFFFFF", 14, 30, 22);
            var lpS = new LinearLayout.LayoutParams(-1, -2); lpS.SetMargins(0, 16, 0, 16); btnSave.LayoutParameters = lpS;

            var btnLogout = Btn(a, "🚪 Выйти из аккаунта", "#64748B", "#FFFFFF", 14, 30, 22);
            btnLogout.LayoutParameters = new LinearLayout.LayoutParams(-1, -2);

            box.AddView(inpNick); box.AddView(inpName); box.AddView(inpBirth); box.AddView(inpPhone); box.AddView(btnSave); box.AddView(btnLogout);
            root.AddView(box);

            btnSave.Click += (s, e) =>
            {
                string nk = inpNick.Text?.Trim() ?? "", fn = inpName.Text?.Trim() ?? "", bd = inpBirth.Text?.Trim() ?? "", ph = inpPhone.Text?.Trim() ?? "";
                if (nk.Length < 2 || ph.Length < 10) { Toast.MakeText(a, "Проверьте логин и номер телефона", ToastLength.Short)?.Show(); return; }
                prefs?.Edit()?.PutString("nick", nk)?.PutString("fullname", fn)?.PutString("birth", bd)?.PutString("phone", ph)?.Apply();
                Toast.MakeText(a, "Данные профиля обновлены!", ToastLength.Short)?.Show();
                onBack();
            };

            btnLogout.Click += (s, e) =>
            {
                prefs?.Edit()?.Clear()?.Apply();
                Toast.MakeText(a, "Вы вышли из аккаунта", ToastLength.Short)?.Show();
                a.Recreate(); // Перезапускаем экран на форму входа
            };

            btnBack.Click += (s, e) => onBack();
            return root;
        }

        private EditText MakeInput(Activity a, string hint, InputTypes type, bool dark)
        {
            var ed = new EditText(a) { Hint = hint, InputType = type, TextSize = 15 };
            ed.SetPadding(32, 22, 32, 22); ed.SetTextColor(Color.ParseColor(dark ? "#FFFFFF" : "#1E293B")); ed.SetHintTextColor(Color.ParseColor("#94A3B8"));
            var gd = new GradientDrawable(); gd.SetColor(Color.ParseColor(dark ? "#1E1E24" : "#FFFFFF")); gd.SetCornerRadius(20f);
            ed.Background = gd; var lp = new LinearLayout.LayoutParams(-1, -2); lp.SetMargins(0, 0, 0, 16); ed.LayoutParameters = lp;
            return ed;
        }

        private Button Btn(Activity a, string t, string bg, string fg, float sz, int px, int py)
        {
            var b = new Button(a) { Text = t, TextSize = sz }; b.SetPadding(px, py, px, py); b.SetMinimumWidth(0); b.SetMinimumHeight(0);
            var gd = new GradientDrawable(); gd.SetColor(Color.ParseColor(bg)); gd.SetCornerRadius(24f); b.Background = gd; b.SetTextColor(Color.ParseColor(fg));
            return b;
        }
    }
}

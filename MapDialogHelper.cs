using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Text;
using Android.Webkit;
using Android.Widget;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace one_circle
{
    public class MapDialogHelper
    {
        public void ShowMap(WebView? w, double la, double lo, double z)
        {
            string sLa = la.ToString(CultureInfo.InvariantCulture), sLo = lo.ToString(CultureInfo.InvariantCulture);
            string p = "https" + "://", d = "www.openstreetmap" + ".org";
            w?.LoadUrl($"{p}{d}/export/embed.html?bbox={(lo - z).ToString(CultureInfo.InvariantCulture)}%2C{(la - z).ToString(CultureInfo.InvariantCulture)}%2C{(lo + z).ToString(CultureInfo.InvariantCulture)}%2C{(la + z).ToString(CultureInfo.InvariantCulture)}&layer=mapnik&marker={sLa}%2C{sLo}");
        }

        public void ShowInvite(Activity a, double lat, double lon, bool hasLoc, Action<double, double> onAdded)
        {
            MainActivity.ShowThemedDialog(a, "Подключение к кругу семьи", (box, dlg) =>
            {
                bool sd = MainActivity.IsSystemDark(a);
                var prefs = a.GetSharedPreferences("oc_auth", FileCreationMode.Private);
                string myPh = prefs?.GetString("phone", "") ?? "";

                var info = new TextView(a) { Text = $"Ваш номер (Хаб): {myPh}\nВведите номер создателя круга, чтобы объединить GPS-карту и чат:", TextSize = 13 };
                info.SetTextColor(Color.ParseColor(sd ? "#94A3B8" : "#475569")); info.SetPadding(0, 0, 0, 18); box.AddView(info);

                var inpPhone = new EditText(a) { Hint = "Телефон главного устройства (+7...)", InputType = InputTypes.ClassPhone, TextSize = 14 };
                inpPhone.SetPadding(24, 16, 24, 16); inpPhone.SetTextColor(Color.ParseColor(sd ? "#FFFFFF" : "#1E293B")); inpPhone.SetHintTextColor(Color.ParseColor("#94A3B8"));
                var gd = new GradientDrawable(); gd.SetColor(Color.ParseColor(sd ? "#27272A" : "#F1F5F9")); gd.SetCornerRadius(20f); inpPhone.Background = gd;
                var lpP = new LinearLayout.LayoutParams(-1, -2); lpP.SetMargins(0, 0, 0, 14); inpPhone.LayoutParameters = lpP;
                box.AddView(inpPhone);

                void AddBtn(string txt, string bg, string fg, Action act)
                {
                    var b = ChatNet.Btn(a, txt, bg, fg, 14, 24, 16);
                    var lp = new LinearLayout.LayoutParams(-1, -2); lp.SetMargins(0, 10, 0, 0); b.LayoutParameters = lp;
                    b.Click += (s, e) => { dlg?.Dismiss(); act(); };
                    box.AddView(b);
                }

                AddBtn("Подключиться к кругу", "#16A34A", "#FFFFFF", async () =>
                {
                    string ph = CircleSync.CleanPhone(inpPhone.Text ?? "");
                    if (ph.Length < 10) { Toast.MakeText(a, "Введите корректный номер (10+ цифр)", ToastLength.Short)?.Show(); return; }
                    CircleSync.ConnectToHub(a, ph);
                    if (hasLoc) await CircleSync.PushMyLocation(a, lat, lon);
                    await CircleSync.PullMembers(a);
                    Toast.MakeText(a, "Подключено к семейному узлу!", ToastLength.Long)?.Show();
                    if (CircleSync.Members.Count > 0) onAdded(CircleSync.Members[0].Lat, CircleSync.Members[0].Lon);
                });

                AddBtn("Сделать мой телефон главным Хабом", "#2563EB", "#FFFFFF", async () =>
                {
                    CircleSync.ConnectToHub(a, myPh);
                    if (hasLoc) await CircleSync.PushMyLocation(a, lat, lon);
                    Toast.MakeText(a, "Ваш телефон теперь главный узел семьи!", ToastLength.Short)?.Show();
                });

                AddBtn("Отмена", sd ? "#27272A" : "#F1F5F9", sd ? "#94A3B8" : "#475569", () => { });
            });
        }

        public void ShowMembers(Activity a, double lat, double lon, bool hasLoc, Action onMe, Action<double, double> onOther)
        {
            _ = Task.Run(async () => await CircleSync.PullMembers(a));
            CircleSync.LoadLocal(a);

            MainActivity.ShowThemedDialog(a, "Участники семейного круга", (box, dlg) =>
            {
                bool sd = MainActivity.IsSystemDark(a);
                string myClean = CircleSync.CleanPhone(a.GetSharedPreferences("oc_auth", FileCreationMode.Private)?.GetString("phone", "") ?? "");

                void AddRow(string txt, string bg, string fg, Action act)
                {
                    var b = ChatNet.Btn(a, txt, bg, fg, 14, 24, 16);
                    var lp = new LinearLayout.LayoutParams(-1, -2); lp.SetMargins(0, 0, 0, 12); b.LayoutParameters = lp;
                    b.Click += (s, e) => { dlg?.Dismiss(); act(); };
                    box.AddView(b);
                }

                AddRow(hasLoc ? $"📍 Вы ({lat:F5}, {lon:F5})" : "⏳ Поиск GPS...", "#2563EB", "#FFFFFF", () => { if (hasLoc) onMe(); });
                foreach (var m in CircleSync.Members)
                {
                    if (m.Phone == myClean) continue;
                    var mem = m;
                    AddRow($"👤 {mem.Name} ({mem.Updated}) • {mem.Lat:F4}, {mem.Lon:F4}", sd ? "#334155" : "#E2E8F0", sd ? "#FFFFFF" : "#1E293B", () => onOther(mem.Lat, mem.Lon));
                }
                AddRow("Закрыть", sd ? "#27272A" : "#F1F5F9", sd ? "#94A3B8" : "#475569", () => { });
            });
        }
    }
}

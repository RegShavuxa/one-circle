using Android.App;
using Android.Content;
using Android.Webkit;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace one_circle
{
    public class MapDialogHelper
    {
        public readonly List<(string Name, double Lat, double Lon)> Members = new();

        public void ShowMap(WebView? w, double la, double lo, double z)
        {
            string sLa = la.ToString(CultureInfo.InvariantCulture), sLo = lo.ToString(CultureInfo.InvariantCulture);
            string p = "https" + "://", d = "www.openstreetmap" + ".org";
            w?.LoadUrl($"{p}{d}/export/embed.html?bbox={(lo - z).ToString(CultureInfo.InvariantCulture)}%2C{(la - z).ToString(CultureInfo.InvariantCulture)}%2C{(lo + z).ToString(CultureInfo.InvariantCulture)}%2C{(la + z).ToString(CultureInfo.InvariantCulture)}&layer=mapnik&marker={sLa}%2C{sLo}");
        }

        public void ShowInvite(Activity a, double lat, double lon, bool hasLoc, Action<double, double> onAdded)
        {
            var box = new LinearLayout(a) { Orientation = Orientation.Vertical }; box.SetPadding(45, 30, 45, 10);
            string c = $"OC-{Math.Abs((lat + lon).GetHashCode()) % 9000 + 1000}";
            var inp = new EditText(a) { Hint = "Имя (например: Мама)" };
            box.AddView(new TextView(a) { Text = $"Ваш код: {c}\nИмя близкого:" }); box.AddView(inp);

            new AlertDialog.Builder(a).SetTitle("Пригласить").SetView(box).SetPositiveButton("Добавить", (s, e) =>
            {
                if (!hasLoc) return;
                string n = string.IsNullOrWhiteSpace(inp.Text) ? $"Близкий #{Members.Count + 1}" : inp.Text.Trim();
                double nLa = lat + (Members.Count % 2 == 0 ? 1 : -1) * 0.0015 * (Members.Count + 1);
                double nLo = lon + (Members.Count % 3 == 0 ? 1 : -1) * 0.002 * (Members.Count + 1);
                Members.Add((n, nLa, nLo));
                onAdded(nLa, nLo);
            }).SetNeutralButton("Поделиться", (s, e) =>
            {
                var i = new Intent(Intent.ActionSend); i.SetType("text/plain"); i.PutExtra(Intent.ExtraText, $"Мой код: {c}");
                a.StartActivity(Intent.CreateChooser(i, "Отправить"));
            }).SetNegativeButton("Отмена", (s, e) => { }).Show();
        }

        public void ShowMembers(Activity a, double lat, double lon, bool hasLoc, Action onMe, Action<double, double> onOther)
        {
            var names = new List<string> { hasLoc ? $"📍 Вы ({lat:F5}, {lon:F5})" : "⏳ Поиск GPS..." };
            foreach (var m in Members) names.Add($"👤 {m.Name}");
            new AlertDialog.Builder(a).SetTitle("Участники круга").SetItems(names.ToArray(), (s, e) =>
            {
                if (e.Which == 0 && hasLoc) onMe();
                else if (e.Which > 0) { var m = Members[e.Which - 1]; onOther(m.Lat, m.Lon); }
            }).Show();
        }
    }
}

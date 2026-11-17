using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Locations;
using Android.OS;
using Android.Views;
using Android.Widget;
using System;

namespace one_circle
{
    [Activity(Label = "one's circle", MainLauncher = true, Exported = true, LaunchMode = LaunchMode.SingleTask)]
    [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "https", DataHost = "ntfy.sh", DataPathPattern = "/ocjoin-.*")]
    [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "onescircle", DataHost = "join")]
    public class MainActivity : Activity, ILocationListener
    {
        public static bool IsSystemDark(Context c)
        {
            var night = (c.Resources?.Configuration?.UiMode ?? Android.Content.Res.UiMode.NightUndefined) & Android.Content.Res.UiMode.NightMask;
            return night == Android.Content.Res.UiMode.NightYes;
        }

        public static AlertDialog ShowThemedDialog(Activity a, string title, Action<LinearLayout, AlertDialog?> buildBody)
        {
            bool dark = IsSystemDark(a);
            var dlg = new AlertDialog.Builder(a).Create()!;
            var box = new LinearLayout(a) { Orientation = Android.Widget.Orientation.Vertical };
            box.SetPadding(40, 36, 40, 36);
            var bg = new GradientDrawable(); bg.SetColor(Color.ParseColor(dark ? "#1E1E24" : "#FFFFFF")); bg.SetCornerRadius(28f);
            box.Background = bg;

            var tit = new TextView(a) { Text = title, TextSize = 17 };
            tit.SetTypeface(null, TypefaceStyle.Bold); tit.SetTextColor(Color.ParseColor(dark ? "#FFFFFF" : "#1E293B")); tit.SetPadding(0, 0, 0, 24);
            box.AddView(tit);
            buildBody(box, dlg);

            dlg.SetView(box); dlg.Show();
            dlg.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
            return dlg;
        }

        private LocationManager? locMgr;
        private readonly CircleHelper ui = new();
        private readonly AuthHelper auth = new();
        private bool openChatOnStart;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            RequestWindowFeature(WindowFeatures.NoTitle);
            HandleInviteIntent(Intent);
            locMgr = (LocationManager?)GetSystemService(Context.LocationService);

            if (auth.IsLoggedIn(this)) OpenMainApp();
            else SetContentView(auth.BuildAuthUI(this, OpenMainApp));
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            HandleInviteIntent(intent);
            if (openChatOnStart && auth.IsLoggedIn(this)) { openChatOnStart = false; ui.OpenChat(this); }
        }

        private void HandleInviteIntent(Intent? intent)
        {
            string? url = intent?.DataString;
            if (!string.IsNullOrEmpty(url) && ChatSecurity.TryJoinByUrl(this, url, out var r, out _))
            {
                GetSharedPreferences("oc_chat", FileCreationMode.Private)?.Edit()?.Clear()?.Apply();
                openChatOnStart = true;
                Toast.MakeText(this, $"Вход в защищенный чат: {r}", ToastLength.Long)?.Show();
            }
        }

        private void OpenMainApp()
        {
            SetContentView(ui.BuildUI(this, StartTracking));
            StartTracking();
            if (openChatOnStart) { openChatOnStart = false; ui.OpenChat(this); }
        }

        public void StartTracking()
        {
            if (CheckSelfPermission(Manifest.Permission.AccessFineLocation) != Permission.Granted) { RequestPermissions(new[] { Manifest.Permission.AccessFineLocation, Manifest.Permission.AccessCoarseLocation }, 101); return; }
            Location? best = null;
            if (locMgr != null)
            {
                foreach (var p in locMgr.AllProviders)
                {
                    var l = locMgr.GetLastKnownLocation(p);
                    if (l != null && (best == null || l.Time > best.Time)) best = l;
                    if (locMgr.IsProviderEnabled(p)) locMgr.RequestLocationUpdates(p, 1000, 1, this);
                }
            }
            if (best != null) ui.UpdateLocation(best.Latitude, best.Longitude, true);
        }

        public override void OnRequestPermissionsResult(int req, string[] p, Permission[] res) { base.OnRequestPermissionsResult(req, p, res); if (req == 101 && Array.IndexOf(res, Permission.Granted) >= 0) StartTracking(); }
        public void OnLocationChanged(Location l) => ui.UpdateLocation(l.Latitude, l.Longitude, false);
        protected override void OnPause() { base.OnPause(); locMgr?.RemoveUpdates(this); }
        protected override void OnResume() { base.OnResume(); if (auth.IsLoggedIn(this) && CheckSelfPermission(Manifest.Permission.AccessFineLocation) == Permission.Granted) StartTracking(); }
        public void OnProviderDisabled(string p) { }
        public void OnProviderEnabled(string p) { }
        public void OnStatusChanged(string? p, Availability s, Bundle? e) { }
    }
}

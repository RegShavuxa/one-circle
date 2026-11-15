using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using Android.Views;

namespace one_circle
{
    [Activity(Label = "one's circle", MainLauncher = true)]
    public class MainActivity : Activity, ILocationListener
    {
        private LocationManager? locMgr;
        private readonly CircleHelper ui = new();
        private readonly AuthHelper auth = new();

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            RequestWindowFeature(WindowFeatures.NoTitle);
            locMgr = (LocationManager?)GetSystemService(Context.LocationService);

            if (auth.IsLoggedIn(this)) OpenMainApp();
            else SetContentView(auth.BuildAuthUI(this, OpenMainApp));
        }

        private void OpenMainApp()
        {
            SetContentView(ui.BuildUI(this, StartTracking));
            StartTracking();
        }

        public void StartTracking()
        {
            if (CheckSelfPermission(Manifest.Permission.AccessFineLocation) != Permission.Granted)
            {
                RequestPermissions(new[] { Manifest.Permission.AccessFineLocation, Manifest.Permission.AccessCoarseLocation }, 101);
                return;
            }
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

        public override void OnRequestPermissionsResult(int req, string[] p, Permission[] res)
        {
            base.OnRequestPermissionsResult(req, p, res);
            if (req == 101 && System.Array.IndexOf(res, Permission.Granted) >= 0) StartTracking();
        }

        public void OnLocationChanged(Location l) => ui.UpdateLocation(l.Latitude, l.Longitude, false);
        protected override void OnPause() { base.OnPause(); locMgr?.RemoveUpdates(this); }
        protected override void OnResume() { base.OnResume(); if (auth.IsLoggedIn(this) && CheckSelfPermission(Manifest.Permission.AccessFineLocation) == Permission.Granted) StartTracking(); }
        public void OnProviderDisabled(string p) { }
        public void OnProviderEnabled(string p) { }
        public void OnStatusChanged(string? p, Availability s, Bundle? e) { }
    }
}

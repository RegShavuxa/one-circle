using Android.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace one_circle
{
    public class MemberInfo
    {
        public string Phone { get; set; } = "";
        public string Name { get; set; } = "";
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Updated { get; set; } = "";
    }

    public static class CircleSync
    {
        private static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(5) };
        public static List<MemberInfo> Members { get; private set; } = new();

        public static string CleanPhone(string p)
        {
            string d = new string((p ?? "").Where(char.IsDigit).ToArray());
            return d.Length >= 10 ? d.Substring(d.Length - 10) : d;
        }

        private static (string Topic, string Key) GetHubChannel(Context c)
        {
            var p = c.GetSharedPreferences("oc_auth", FileCreationMode.Private);
            string myPh = CleanPhone(p?.GetString("phone", "") ?? "");
            string hubPh = CleanPhone(p?.GetString("hub_phone", "") ?? "");
            if (string.IsNullOrEmpty(hubPh)) hubPh = myPh;
            return GetHubChannelFor(hubPh);
        }

        public static void ConnectToHub(Context c, string hubPhone)
        {
            string h = CleanPhone(hubPhone);
            if (h.Length < 10) return;
            var (topic, key) = GetHubChannelFor(h);
            c.GetSharedPreferences("oc_auth", FileCreationMode.Private)?.Edit()?
                .PutString("hub_phone", h)?.PutString("room", topic + "chat")?.PutString("room_key", key)?.Apply();
            c.GetSharedPreferences("oc_chat", FileCreationMode.Private)?.Edit()?.Clear()?.Apply();
        }

        private static (string Topic, string Key) GetHubChannelFor(string cleanPhone)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"hub:{cleanPhone}:oc2026"));
            string hex = Convert.ToHexString(hash).ToLower();
            return ("ochub" + hex.Substring(0, 10), hex.Substring(10, 12));
        }

        public static void LoadLocal(Context c)
        {
            try
            {
                string j = c.GetSharedPreferences("oc_hub", FileCreationMode.Private)?.GetString("members", "") ?? "";
                if (!string.IsNullOrEmpty(j)) Members = JsonSerializer.Deserialize<List<MemberInfo>>(j) ?? new();
            }
            catch { }
        }

        private static void SaveLocal(Context c) =>
            c.GetSharedPreferences("oc_hub", FileCreationMode.Private)?.Edit()?.PutString("members", JsonSerializer.Serialize(Members))?.Apply();

        public static async Task PushMyLocation(Context c, double lat, double lon)
        {
            try
            {
                var p = c.GetSharedPreferences("oc_auth", FileCreationMode.Private);
                string myPh = CleanPhone(p?.GetString("phone", "") ?? "");
                string nick = p?.GetString("nick", "Участник") ?? "Участник";
                if (myPh.Length < 10) return;

                var me = new MemberInfo { Phone = myPh, Name = nick, Lat = lat, Lon = lon, Updated = DateTime.Now.ToString("HH:mm") };
                Upsert(me); SaveLocal(c);

                var (topic, key) = GetHubChannel(c);
                string enc = ChatSecurity.Encrypt(JsonSerializer.Serialize(me), key);
                await http.PostAsync("https" + "://" + "ntfy" + $".sh/{topic}", new StringContent(enc, Encoding.UTF8, "text/plain"));
            }
            catch { }
        }

        public static async Task PullMembers(Context c)
        {
            try
            {
                var (topic, key) = GetHubChannel(c);
                string raw = await http.GetStringAsync("https" + "://" + "ntfy" + $".sh/{topic}/json?poll=1&since=12h");
                bool changed = false;
                foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    using var doc = JsonDocument.Parse(line);
                    if (!doc.RootElement.TryGetProperty("message", out var mp)) continue;
                    string dec = ChatSecurity.Decrypt(mp.GetString() ?? "", key);
                    var m = string.IsNullOrEmpty(dec) ? null : JsonSerializer.Deserialize<MemberInfo>(dec);
                    if (m != null && !string.IsNullOrEmpty(m.Phone)) { Upsert(m); changed = true; }
                }
                if (changed) SaveLocal(c);
            }
            catch { }
        }

        private static void Upsert(MemberInfo m)
        {
            int idx = Members.FindIndex(x => x.Phone == m.Phone);
            if (idx >= 0) Members[idx] = m;
            else Members.Add(m);
        }
    }
}

using Android.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace one_circle
{
    public class ChatRoomItem { public string Title { get; set; } = ""; public string Room { get; set; } = ""; public string Key { get; set; } = ""; }

    public static class ChatSecurity
    {
        private static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(4) };

        public static (string Room, string Key) GetOrCreateRoom(Context c)
        {
            var p = c.GetSharedPreferences("oc_auth", FileCreationMode.Private);
            string hubPh = CircleSync.CleanPhone(p?.GetString("hub_phone", "") ?? "");
            if (hubPh.Length < 10) hubPh = CircleSync.CleanPhone(p?.GetString("phone", "") ?? "");
            if (hubPh.Length >= 10)
            {
                byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"hub:{hubPh}:oc2026"));
                string hex = Convert.ToHexString(hash).ToLower();
                return ("ochub" + hex.Substring(0, 10) + "chat", hex.Substring(10, 12));
            }
            return ("oc-family", "oc2026key");
        }

        public static (string Room, string Key) GetDirectRoom(string myPhone, string peerPhone)
        {
            var pair = new[] { CircleSync.CleanPhone(myPhone), CircleSync.CleanPhone(peerPhone) }.OrderBy(x => x).ToArray();
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"dm:{pair.ElementAt(0)}:{pair.ElementAt(1)}:oc2026"));
            string hex = Convert.ToHexString(hash).ToLower();
            return ("ocdm" + hex.Substring(0, 10), hex.Substring(10, 12));
        }

        public static List<ChatRoomItem> GetAllChats(Context c)
        {
            var list = new List<ChatRoomItem>();
            var (hubRoom, hubKey) = GetOrCreateRoom(c);
            list.Add(new ChatRoomItem { Title = "🏠 Общий чат семьи (Хаб)", Room = hubRoom, Key = hubKey });

            string myClean = CircleSync.CleanPhone(c.GetSharedPreferences("oc_auth", FileCreationMode.Private)?.GetString("phone", "") ?? "");
            CircleSync.LoadLocal(c);
            foreach (var m in CircleSync.Members)
            {
                if (m.Phone == myClean || m.Phone.Length < 10) continue;
                var (dr, dk) = GetDirectRoom(myClean, m.Phone);
                if (!list.Exists(x => x.Room == dr)) list.Add(new ChatRoomItem { Title = $"👤 {m.Name} ({m.Phone})", Room = dr, Key = dk });
            }

            try
            {
                string j = c.GetSharedPreferences("oc_chat", FileCreationMode.Private)?.GetString("extra_rooms", "") ?? "";
                var extra = string.IsNullOrEmpty(j) ? new() : (JsonSerializer.Deserialize<List<ChatRoomItem>>(j) ?? new());
                foreach (var e in extra) if (!list.Exists(x => x.Room == e.Room)) list.Add(e);
            }
            catch { }
            return list;
        }

        public static void SaveExtraChat(Context c, string title, string room, string key)
        {
            try
            {
                var p = c.GetSharedPreferences("oc_chat", FileCreationMode.Private);
                var list = JsonSerializer.Deserialize<List<ChatRoomItem>>(p?.GetString("extra_rooms", "") ?? "") ?? new();
                if (!list.Exists(x => x.Room == room)) { list.Add(new ChatRoomItem { Title = title, Room = room, Key = key }); p?.Edit()?.PutString("extra_rooms", JsonSerializer.Serialize(list))?.Apply(); }
            }
            catch { }
        }

        public static string MakeInviteUrl(string room, string key)
        {
            string target = Uri.EscapeDataString("https://youtube.com");
            try
            {
                string shortBase = Task.Run(() => http.GetStringAsync($"https://clck.ru{target}")).GetAwaiter().GetResult().Trim();
                if (shortBase.StartsWith("https://clck.ru")) return $"{shortBase}#ocjoin-{room}-{key}";
            }
            catch { }
            return $"https://clck.ru32x8kL#ocjoin-{room}-{key}";
        }

        public static bool TryJoinByUrl(Context c, string rawText, out string room, out string key)
        {
            room = ""; key = "";
            try
            {
                var m = Regex.Match(rawText, @"ocjoin-([a-zA-Z0-9]+)-([a-zA-Z0-9]+)");
                if (!m.Success) return false;
                room = m.Groups.Cast<Group>().ElementAt(1).Value.ToLower();
                key = m.Groups.Cast<Group>().ElementAt(2).Value.ToLower();
                SaveExtraChat(c, $"💬 Чат {room}", room, key);
                return true;
            }
            catch { return false; }
        }

        public static string Encrypt(string text, string key)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(key)) return text;
            var b = Encoding.UTF8.GetBytes(text); var k = Encoding.UTF8.GetBytes(key);
            for (int i = 0; i < b.Length; i++) b.SetValue((byte)(b.ElementAt(i) ^ k.ElementAt(i % k.Length)), i);
            return Convert.ToBase64String(b);
        }

        public static string Decrypt(string b64, string key)
        {
            try
            {
                var b = Convert.FromBase64String(b64); var k = Encoding.UTF8.GetBytes(key);
                for (int i = 0; i < b.Length; i++) b.SetValue((byte)(b.ElementAt(i) ^ k.ElementAt(i % k.Length)), i);
                return Encoding.UTF8.GetString(b);
            }
            catch { return ""; }
        }
    }
}

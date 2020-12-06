using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MsTeamsBot
{
    class DiscordWebhook
    {
        private static readonly HttpClient _client = new HttpClient();

        private string _webhookURL;
        private string _username;
        private string _avatarUrl;

        public DiscordWebhook(string url) { this._webhookURL = url; }
        public DiscordWebhook(string url, string username) { this._webhookURL = url; this._username = username; }
        public DiscordWebhook(string url, string username, string avatarURL) { this._webhookURL = url; this._username = username; this._avatarUrl = avatarURL; }
        public async Task<HttpResponseMessage> SendMessageAsync(string username = null, string avatarUrl = null, string content = null, bool isTTS = false, Embed embeds = null)
        {
            var msg = new Message(username ?? this._username, avatarUrl ?? this._avatarUrl, content, isTTS, embeds);
            return await _client.PostAsync(this._webhookURL, new StringContent(JsonConvert.SerializeObject(msg), Encoding.UTF8, "application/json"));
        }
        public async Task<HttpResponseMessage> SendMessageAsync(string username = null, string avatarUrl = null, string content = null, bool isTTS = false, IEnumerable<Embed> embeds = null)
        {
            var msg = new Message(username ?? this._username, avatarUrl ?? this._avatarUrl, content, isTTS, embeds);
            return await _client.PostAsync(this._webhookURL, new StringContent(JsonConvert.SerializeObject(msg), Encoding.UTF8, "application/json"));
        }

        [JsonObject]
        internal class Message
        {
            [JsonProperty("username")]
            public string Username;
            [JsonProperty("avatar_url")]
            public string AvatarUrl;
            [JsonProperty("content")]
            public string Content;
            [JsonProperty("tts")]
            public bool isTTS;
            [JsonProperty("embeds")]
            public List<Embed> Embeds;

            public Message(string username, string avatarUrl, string content, bool isTTS, IEnumerable<Embed> embeds)
            {
                this.Username = username;
                this.AvatarUrl = avatarUrl;
                this.Content = content;
                this.isTTS = isTTS;
                Embeds = new List<Embed>(embeds);
            }

            public Message(string username, string avatarUrl, string content, bool isTTS, Embed embed)
            {
                this.Username = username;
                this.AvatarUrl = avatarUrl;
                this.Content = content;
                this.isTTS = isTTS;
                Embeds = new List<Embed>();
                Embeds.Add(embed);
            }
        }
    }
}

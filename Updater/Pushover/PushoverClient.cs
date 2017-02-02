using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Updater.Pushover
{
    public class PushoverClient
    {
        public string ApiKey { get; set; }
        public string UserKey { get; private set; } 
        private readonly WebClient webClient;

        public PushoverClient(string apiKey, string userKey)
        {
            ApiKey = apiKey;
            UserKey = userKey;

            webClient = new WebClient
            {
                Headers =
                {
                    ["token"] = ApiKey,
                    ["user"] = UserKey
                }
            };
        }

        public async Task<bool> PushMessage(PushoverMessage message)
        {
            byte[] byteResponse;

            try
            {
                byteResponse = await webClient.UploadValuesTaskAsync("https://api.pushover.net/1/messages.json", new NameValueCollection
                {
                    { "token", ApiKey },
                    { "user", UserKey },
                    { "message", message.Text },
                    { "title", message.Title },
                    { "url", message.Url },
                    { "url_title", message.UrlTitle },
                    { "priority", ((int)message.Priority).ToString() },
                    { "sound", message.Sound.ToString().ToLower() }
                });
            }
            catch (WebException ex)
            {
                var stream = ex.Response.GetResponseStream();

                if (stream == null)
                    return false;

                if (stream.Length <= 0)
                    return false;

                byteResponse = new byte[stream.Length];
                await stream.ReadAsync(byteResponse, 0, (int) stream.Length);
            }

            string response = Encoding.UTF8.GetString(byteResponse);
            JObject jResponse = JObject.Parse(response);

            return jResponse["status"].Value<int>() == 1;
        }
    }
}
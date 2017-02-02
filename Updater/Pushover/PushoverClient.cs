using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

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
            webClient.Headers["token"] = ApiKey;
            webClient.Headers["user"] = UserKey;
        }

        public async Task<bool> PushMessage(PushoverMessage message)
        {
            webClient.Headers["message"] = message.Message;
            webClient.Headers["title"] = message.Title;
            webClient.Headers["url"] = message.Url;
            webClient.Headers["url_title"] = message.UrlTitle;
            webClient.Headers["priority"] = ((int) message.Priority).ToString();
            webClient.Headers["sound"] = message.Sound.ToString();

            byte[] byteResponse = await webClient.UploadDataTaskAsync("https://api.pushover.net/1/messages.json", new byte[] {});

            return true; // Assume success for now. todo: read response
        }
    }
}
using System;
using System.Threading.Tasks;
using PushbulletSharp;
using PushbulletSharp.Models.Requests;
using PushbulletSharp.Models.Requests.Ephemerals;
using PushbulletSharp.Models.Responses;

namespace Updater
{
    public static class PushBulletExtensions
    {
        /// <summary>Broadcasts an notification ephemeral to all devices.</summary>
        public static PushResponse SendNotification(this PushbulletClient client, string title, string text)
        {
            if (client == null)
                throw new NullReferenceException();

            var currentUser = client.CurrentUsersInformation();

            if (currentUser != null)
            {
                var request = new PushNoteRequest
                {
                    Email = currentUser.Email,
                    Title = title,
                    Body = text
                };

                return client.PushNote(request);
            }

            return null;
        }

        public static Task<PushResponse> SendNotificationAsync(this PushbulletClient client, string title, string text)
        {
            return Task.Run(() => SendNotification(client, title, text));
        }
    }
}
﻿using System;
using System.IO;
using System.Threading.Tasks;
using PushbulletSharp;
using PushbulletSharp.Models.Requests;
using PushbulletSharp.Models.Responses;

namespace Updater.Extensions
{
    public static class PushBulletExtensions
    {
        /// <summary>Tries to send a notification to all devices. Returns null if sent unsuccessfully.</summary>
        public static PushResponse SendNotification(this PushbulletClient client, string title, string text)
        {
            if (client == null) throw new NullReferenceException();
            if (title == null) throw new ArgumentNullException(nameof(title));
            if (text == null) throw new ArgumentNullException(nameof(text));

            try
            {
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
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return null;
            }

            return null;
        }

        /// <summary>Tries to send a notification to all devices. Returns null if sent unsuccessfully.</summary>
        public static Task<PushResponse> SendNotificationAsync(this PushbulletClient client, string title, string text)
        {
            return Task.Run(() => SendNotification(client, title, text));
        }

        public static PushResponse SendFile(this PushbulletClient client, Stream fileStream, string text)
        {
            if (client == null) throw new NullReferenceException();
            if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));

            try
            {
                var currentUser = client.CurrentUsersInformation();

                if (currentUser != null)
                {
                    var request = new PushFileRequest
                    {
                        FileType = "text/plain",
                        FileName = "ErrorLog.txt",
                        Body = text,
                        FileStream = fileStream
                    };

                    return client.PushFile(request);
                }
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return null;
            }

            return null;
        }

        public static Task<PushResponse> SendFileAsync(this PushbulletClient client, Stream fileStream, string text)
        {
            return Task.Run(() => SendFile(client, fileStream, text));
        } 
    }
}
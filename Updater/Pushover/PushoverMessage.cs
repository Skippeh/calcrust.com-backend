namespace Updater.Pushover
{
    public class PushoverMessage
    {
        public string Message;
        public string Title;
        public string Url;
        public string UrlTitle;
        public MessagePriority Priority;
        public MessageSound Sound;

        public PushoverMessage(string message, string title) : this(message, title, MessagePriority.NoNotification) { }
        public PushoverMessage(string message, string title, MessagePriority priority) : this(message, title, priority, MessageSound.None) { }
        public PushoverMessage(string message, string title, MessagePriority priority, MessageSound sound)
        {
            Message = message;
            Title = title;
            Priority = priority;
            Sound = sound;
        }
    }
}
namespace Updater.Pushover
{
    public class PushoverMessage
    {
        public string Text;
        public string Title;
        public string Url;
        public string UrlTitle;
        public MessagePriority Priority;
        public MessageSound Sound;

        public PushoverMessage(string text, string title) : this(text, title, MessagePriority.NoNotification) { }
        public PushoverMessage(string text, string title, MessagePriority priority) : this(text, title, priority, MessageSound.None) { }
        public PushoverMessage(string text, string title, MessagePriority priority, MessageSound sound)
        {
            Text = text;
            Title = title;
            Priority = priority;
            Sound = sound;
        }
    }
}
using System;

namespace Updater.Steam
{
    public class UpdateInfo
    {
        public uint BuildID;

        /// <summary>The time the update was released in UTC.</summary>
        public DateTime TimeUpdated;
    }
}
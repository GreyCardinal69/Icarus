using System;
using System.Collections.Generic;
using Icarus.Modules.Isolation;
using Icarus.Modules.Profiles;

namespace Icarus.Modules
{
    public sealed class ServerProfile
    {
        public ServerProfile () { }

        public string Name;
        public ulong ID;
        public LogProfile LogConfig = new();
        public AntiSpamProfile AntiSpam = new() { CacheResetInterval = 20, FirstWarning = 5, LastWarning = 9, Limit = 12, SecondWarning = 7 };
        public List<IsolationEntry> Entries = new();
        public DateTime ProfileCreationDate { get; init; }

        public void SetContainmentDefaults( ulong channelId, ulong roleId )
        {
            LogConfig.DefaultContainmentChannelId = channelId;
            LogConfig.DefaultContainmentRoleId = roleId;
        }

        public static ServerProfile ProfileFromId ( ulong ID )
        {
            var ProfileList = Program.Core.ServerProfiles;
            foreach (ServerProfile prof in ProfileList)
            {
                if (prof.ID == ID)
                {
                    return prof;
                }
            }
            return null;
        }
    }
}
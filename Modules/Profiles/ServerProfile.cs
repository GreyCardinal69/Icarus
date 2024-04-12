using Icarus.Modules.Isolation;
using Icarus.Modules.Profiles;
using System;
using System.Collections.Generic;

namespace Icarus.Modules
{
    public sealed class ServerProfile
    {
        public ServerProfile () { }

        public string Name;
        public ulong ID;
        public LogProfile LogConfig = new LogProfile();
        public AntiSpamProfile AntiSpam = new AntiSpamProfile() { CacheResetInterval = 20, FirstWarning = 5, LastWarning = 9, Limit = 12, SecondWarning = 7 };
        public List<IsolationEntry> Entries = new List<IsolationEntry>();
        public List<ulong> AntiSpamIgnored = new List<ulong>();
        public List<string> WordBlackList = new List<string>();
        public DateTime ProfileCreationDate { get; init; }
        public Dictionary<DateTime, ServerLog> WeeklyLogs = new Dictionary<DateTime, ServerLog>();
        public List<TimedReminder> TimedReminders = new List<TimedReminder>();


        public bool HasCustomWelcome => _hasCustomWelcome;
        public UserWelcome CustomWelcome => _userWelcome;

        private bool _hasCustomWelcome;
        private UserWelcome _userWelcome;

        public void SetCustomWelcome( UserWelcome content )
        {
            _userWelcome = content;
            _hasCustomWelcome = true;
        }

        public void RemoveCustomWelcome()
        {
            _hasCustomWelcome = false;
            _userWelcome = new UserWelcome() { };
        }

        public void SetContainmentDefaults( ulong channelId, ulong roleId )
        {
            LogConfig.DefaultContainmentChannelId = channelId;
            LogConfig.DefaultContainmentRoleId = roleId;
        }

        public static ServerProfile ProfileFromId ( ulong ID )
        {
            List<ServerProfile> ProfileList = Program.Core.ServerProfiles;
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
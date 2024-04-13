using Icarus.Modules.Isolation;
using Icarus.Modules.Profiles;
using Newtonsoft.Json;
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
        public List<IsolationEntry> Entries = new List<IsolationEntry>();
        public List<ulong> AntiSpamIgnored = new List<ulong>();
        public List<string> WordBlackList = new List<string>();
        public DateTime ProfileCreationDate { get; init; }
        public List<TimedReminder> TimedReminders = new List<TimedReminder>();

        [JsonIgnore] public AntiSpamProfile AntiSpamProfile => _antiSpamProfile;
        [JsonIgnore] public bool AntiSpamModuleActive => _antiSpamModuleActive;
        [JsonIgnore] public bool HasCustomWelcome => _hasCustomWelcome;
        [JsonIgnore] public UserWelcome CustomWelcome => _userWelcome;
        [JsonIgnore] public ServerLog CurrentWeeklyLog => _currentWeeklyLog;

        [JsonProperty] private bool _hasCustomWelcome;
        [JsonProperty] private UserWelcome _userWelcome;
        [JsonProperty] private bool _antiSpamModuleActive;
        [JsonProperty] private AntiSpamProfile _antiSpamProfile;
        [JsonProperty] private ServerLog _currentWeeklyLog;

        public void EnableAntiSpam( int interval, int first, int second, int last, int limit )
        {
            _antiSpamProfile = new AntiSpamProfile() { CacheResetInterval = interval, FirstWarning = first, LastWarning = last, Limit = limit, SecondWarning = second };
            _antiSpamModuleActive = true;
        }

        public void DisableAntiSpamModule()
        {
            _antiSpamModuleActive = false;
            _antiSpamProfile = new AntiSpamProfile();
        }

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
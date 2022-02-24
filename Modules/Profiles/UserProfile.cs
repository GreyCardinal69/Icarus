using System;
using System.Collections.Generic;

namespace Icarus.Modules.Profiles
{
    public class UserProfile
    {
        public ulong ID { get; set; }

        public string Discriminator { get; set; }
        public string LastUsername { get; set; }
        public string LocalLanguage { get; set; }
        public List<string> OldUsernames { get; set; }

        public List<Tuple<DateTime, string>> PunishmentEntries { get; set; }
        public List<Tuple<DateTime, string>> BanEntries { get; set; }
        public List<Tuple<DateTime, string>> KickEntries { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public DateTime LastJoinDate { get; set; }
        public DateTimeOffset FirstJoinDate { get; set; }
        public DateTime LeaveDate { get; set; }

        public UserProfile ( ulong id, string usernameLast )
        {
            OldUsernames = new();
            PunishmentEntries = new();
            BanEntries = new();
            KickEntries = new();

            ID = id;
            LastUsername = usernameLast;
        }
    }
}
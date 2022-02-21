using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Modules.Profiles
{
    public class UserProfile
    {
        public ulong ID { get; set; }

        public string LastUsername { get; set; }
        public List<string> OldUsernames { get; set; }

        public List<Tuple<int, string>> Strikes;

        public List<Tuple<DateTime, string>> PunishmentEntries { get; set; }
        public List<Tuple<DateTime, string>> BanEntries { get; set; }
        public List<Tuple<DateTime, string>> KickEntries { get; set; }

        public DateTime CreationDate { get; set; }
        public DateTime LastJoinDate { get; set; }
        public DateTime FirstJoinDate { get; set; }
        public DateTime LeaveDate { get; set; }

        public UserProfile ( ulong id, string usernameLast )
        {
            Strikes = new();
            OldUsernames = new();
            PunishmentEntries = new();
            BanEntries = new();
            KickEntries = new();

            ID = id;
            LastUsername = usernameLast;
        }
    }
}
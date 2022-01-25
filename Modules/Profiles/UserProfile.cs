using System;
using System.Collections.Generic;

namespace Icarus.Modules.Profiles
{
    public class UserProfile
    {
        public ulong ID { get; set; }

        public string LastUsername { get; set; }
        public List<string> OldUsernames { get; set; }

        public int SmallWarnings { get; set; }
        public int MediumWarnings { get; set; }
        public int SeriousWarnings { get; set; }

        public List<Tuple<DateTimeOffset, string>> Bans { get; set; }
        public List<Tuple<DateTimeOffset, string>> Kicks { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset LastJoinDate { get; set; }
        public DateTimeOffset RemovedDate { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Icarus.Modules.Isolation;
using Icarus.Modules.Profiles;

namespace Icarus.Modules
{
    public sealed class ServerProfile
    {
        public ServerProfile () { }

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

        public string Name;
        public ulong ID;
        public LogProfile LogConfig = new LogProfile();
        public List<IsolationEntry> Entries = new List<IsolationEntry>();
        public DateTime ProfileCreationDate { get; init; }
    }
}
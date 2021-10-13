using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Icarus.Modules.Logs;

namespace Icarus.Modules.Profiles
{
    public class ServerProfile
    {
        public ServerProfile () { }

        public string Name { get; set; }
        public ulong ID { get; set; }
        public LogConfig LogConfig { get; set; }

        public static ServerProfile ProfileFromId( ulong ID )
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
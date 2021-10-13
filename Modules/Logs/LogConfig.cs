using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Modules.Logs
{
    public class LogConfig
    {
        public void ToggleLogging( bool Enabled )
        {
            this.LoggingEnabled = Enabled;
        }

        public static List<string> LogEvents () => new()
        { 
          "GuildMemberRemoved", "GuildMemberAdded", "GuildBanRemoved", "GuildBanAdded",
          "GuildRoleCreated","GuildRoleUpdated", "GuildRoleDeleted", "MessageReactionsCleared",
          "MessageReactionRemoved", "MessageReactionAdded", "MessagesBulkDeleted","MessageDeleted",
          "MessageUpdated", "InviteDeleted", "InviteCreated", "ChannelUpdated",
          "ChannelDeleted", "ChannelCreated"
        };

        public bool LoggingEnabled { get; set; }
        public bool GuildMemberRemoved { get; set; }
        public bool GuildMemberAdded { get; set; }
        public bool GuildBanRemoved { get; set; }
        public bool GuildBanAdded { get; set; }
        public bool GuildRoleCreated { get; set; }
        public bool GuildRoleUpdated { get; set; }
        public bool GuildRoleDeleted { get; set; }
        public bool MessageReactionsCleared { get; set; }
        public bool MessageReactionRemoved { get; set; }
        public bool MessageReactionAdded { get; set; }
        public bool MessagesBulkDeleted { get; set; }
        public bool MessageDeleted { get; set; }
        public bool MessageUpdated { get; set; }
        public bool MessageCreated { get; set; }
        public bool InviteDeleted { get; set; }
        public bool InviteCreated { get; set; }
        public bool ChannelUpdated { get; set; }
        public bool ChannelDeleted { get; set; }
        public bool ChannelCreated { get; set; }
        public ulong LogChannel { get; set; }
        public ulong SecondaryLogChannel { get; set; }
    }
}
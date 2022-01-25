using DSharpPlus.Entities;
using System;
using System.Collections.Generic;

namespace Icarus.Modules.Isolation
{
    public class IsolationEntry
    {
        public string IsolationChannel;
        public string Reason;
        public ulong GuildID;
        public ulong PurgeRoleID;
        public ulong BackUpChannelID;
        public ulong ChannelCallID;
        public ulong MessageID;
        public ulong IsolatedUserID;
        public DateTimeOffset EntryDate;
        public DateTimeOffset ReleaseDate;
        public List<DiscordRole> RemovedRoles = new();
    }
}
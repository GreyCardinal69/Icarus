using System;
using System.Collections.Generic;

using DSharpPlus.Entities;

namespace Icarus.Modules.Isolation
{
    public struct IsolationEntry
    {
        public string IsolationChannel;
        public ulong IsolationChannelId;
        public ulong PunishmentRoleId;
        public string EntryMessageLink;
        public ulong IsolatedUserId;
        public string IsolatedUserName;
        public DateTime EntryDate;
        public DateTime ReleaseDate;
        public List<DiscordRole> RemovedRoles;
        public bool ReturnRoles;
        public string Reason;
    }
}
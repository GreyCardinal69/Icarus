﻿using System;
using System.Collections.Generic;

namespace Icarus.Modules.Profiles
{
    public class UserProfile
    {
        public ulong ID { get; set; }

        public string Discriminator { get; set; }
        public string LocalLanguage { get; set; }

        public List<(DateTime, string)> PunishmentEntries { get; set; }
        public List<(DateTime, string)> BanEntries { get; set; }
        public List<(DateTime, string)> KickEntries { get; set; }

        public Dictionary<int, string> Notes {  get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public DateTime LastJoinDate { get; set; }
        public DateTimeOffset FirstJoinDate { get; set; }
        public DateTime LeaveDate { get; set; }

        public UserProfile ( ulong id )
        {
            PunishmentEntries = new List<(DateTime, string)>();
            BanEntries = new List<(DateTime, string)>();
            KickEntries = new List<(DateTime, string)>();
            Notes = new Dictionary<int, string>();

            ID = id;
        }
    }
}
﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Icarus.Modules.Profiles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Icarus.Modules.Logs
{
    public class UserLogging : BaseCommandModule
    {
        [Command( "RegisterUsers" )]
        [Description( "Creates profiles for all server users." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.Administrator )]
        public async Task RegisterUsers( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();

            IReadOnlyCollection<DiscordMember> users = ctx.Guild.GetAllMembersAsync().Result;

            int i = 0;
            foreach ( DiscordMember user in users )
            {
                if ( !File.Exists( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{user.Id}.json" ) )
                {
                    i++;
                    UserProfile profile = new UserProfile( user.Id )
                    {
                        Discriminator = user.Discriminator,
                        CreationDate = user.CreationTimestamp,
                        FirstJoinDate = user.JoinedAt,
                        LocalLanguage = user.Locale,
                    };
                    Directory.CreateDirectory( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\" );
                    File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{user.Id}.json",
                        JsonConvert.SerializeObject( profile, Formatting.Indented ) );
                }
            }

            await ctx.RespondAsync( $"Created {i} user profiles." );
        }

        [Command( "AddUserNote" )]
        [Description( "Responds with information on a user's profile." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageRoles )]
        public async Task AddUserNote( CommandContext ctx, ulong id, int index, params string[] note )
        {
            await ctx.TriggerTypingAsync();

            if ( ctx.Guild.GetMemberAsync( id ).Result == null )
            {
                await ctx.RespondAsync( "Invalid User Id." );
                return;
            }

            UserProfile user = JsonConvert.DeserializeObject<UserProfile>(
                File.ReadAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{id}.json" ) );

            if ( user.Notes.ContainsKey( index ) )
            {
                await ctx.RespondAsync( $"A user note with index {index} already exists." );
                return;
            }

            user.Notes.Add( index, string.Join( " ", note ) );

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{id}.json",
              JsonConvert.SerializeObject( user, Formatting.Indented ) );

            await ctx.RespondAsync( "Note added." );
        }

        [Command( "RemoveUserNote" )]
        [Description( "Responds with information on a user's profile." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageRoles )]
        public async Task RemoveUserNote( CommandContext ctx, ulong id, int index )
        {
            await ctx.TriggerTypingAsync();

            if ( ctx.Guild.GetMemberAsync( id ).Result == null )
            {
                await ctx.RespondAsync( "Invalid User Id." );
                return;
            }

            var user = JsonConvert.DeserializeObject<UserProfile>(
                File.ReadAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{id}.json" ) );

            if ( !user.Notes.ContainsKey( index ) )
            {
                await ctx.RespondAsync( $"A user note with index {index} doesn't exists." );
                return;
            }

            user.Notes.Remove( index );

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{id}.json",
              JsonConvert.SerializeObject( user, Formatting.Indented ) );

            await ctx.RespondAsync( "Note deleted." );
        }

        [Command( "UserProfile" )]
        [Description( "Responds with information on a user's profile." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageRoles )]
        public async Task UserProfile( CommandContext ctx, ulong id )
        {
            await ctx.TriggerTypingAsync();

            if ( ctx.Guild.GetMemberAsync( id ).Result == null )
            {
                await ctx.RespondAsync( "Invalid User Id." );
                return;
            }

            UserProfile user = JsonConvert.DeserializeObject<UserProfile>(
                File.ReadAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{id}.json" ) );

            List<string> banEntries = new List<string>();
            foreach ( (DateTime, string) item in user.BanEntries )
            {
                banEntries.Add( $"{item.Item1}  :  {item.Item2}" );
            }

            List<string> kickEntries = new List<string>();
            foreach ( (DateTime, string) item in user.KickEntries )
            {
                kickEntries.Add( $"{item.Item1}  :  {item.Item2}" );
            }

            List<string> punishmentEntries = new List<string>();
            foreach ( (DateTime, string) item in user.PunishmentEntries )
            {
                punishmentEntries.Add( $"{item.Item1}  :  {item.Item2}" );
            }

            List<string> noteEntries = new List<string>();
            foreach ( KeyValuePair<int, string> item in user.Notes )
            {
                noteEntries.Add( $"    {item.Key}  :  {item.Value}" );
            }

            string bans = banEntries.Count == 0 ? "None" : string.Join( "\n", banEntries );
            string kicks = kickEntries.Count == 0 ? "None" : string.Join( "\n", kickEntries );
            string strikes = punishmentEntries.Count == 0 ? "None" : $"\n{string.Join( "\n", punishmentEntries )}";
            string notes = noteEntries.Count == 0 ? "None" : $"\n{string.Join( "\n", noteEntries )}";

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder
            {
                Title = $"Profile {ctx.Guild.GetMemberAsync( id ).Result.Username}",
                Color = DiscordColor.SpringGreen,
                Description =
                    $"The user's id is: {user.ID}.\n Discriminator: #{user.Discriminator}.\n The account was created at {user.CreationDate}.\n The user first joined at: {user.FirstJoinDate}.\n The user last left the server at {user.LeaveDate}.\n\n The user's logged ban entries are: {bans}.\n\n The user's logged kick entries are: {kicks}.\n\n The user's logged punishment entries are: {strikes}.\n\n The user has the following notes given by moderators: {notes}.",
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    IconUrl = ctx.Client.CurrentUser.AvatarUrl,
                },
                Timestamp = DateTime.Now
            };

            await ctx.RespondAsync( embed );
        }
    }
}
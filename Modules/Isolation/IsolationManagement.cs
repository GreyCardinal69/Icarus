using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Icarus.Modules.Profiles;
using Newtonsoft.Json;

namespace Icarus.Modules.Isolation
{
    public class IsolationManagement : BaseCommandModule
    {
        [Command( "isolate" )]
        [Description( "Isolates a user in a channel with specified parameters." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageRoles )]
        public async Task Isolate ( CommandContext ctx, ulong punishmentRoleId, ulong userId, ulong channelId, string timeLen, bool returnRoles, [RemainingText] string reason)
        {
            await ctx.TriggerTypingAsync();

            if (!Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ))
            {
                await ctx.RespondAsync( "Server is not registered, call `>RegisterServer` to proceed." );
                return;
            }

            if (ctx.Guild.GetMemberAsync( userId ) == null)
            {
                await ctx.RespondAsync( "Invalid user id." );
                return;
            }

            if (ctx.Guild.GetRole( punishmentRoleId ) == null)
            {
                await ctx.RespondAsync( "Invalid containment role id." );
                return;
            }

            ServerProfile Profile = ServerProfile.ProfileFromId( ctx.Guild.Id );
            var user = ctx.Guild.GetMemberAsync( userId ).Result;

            IsolationEntry NewEntry = new()
            {
                IsolationChannel = ctx.Guild.GetChannel( channelId ).Mention,
                IsolationChannelId = channelId,
                PunishmentRoleId = punishmentRoleId,
                EntryMessageLink = ctx.Message.JumpLink.ToString(),
                IsolatedUserId = userId,
                IsolatedUserName = user.DisplayName,
                EntryDate = DateTime.UtcNow,
                ReleaseDate = timeLen.EndsWith('d')
                        ? DateTime.UtcNow.AddDays( Convert.ToDouble( timeLen[..^1]) )
                        : DateTime.UtcNow.AddMonths( Convert.ToInt32( timeLen[..^1] ) ),
                RemovedRoles = user.Roles.ToList(),
                ReturnRoles = returnRoles
            };

            foreach (var item in user.Roles.ToList())
            {
                await user.RevokeRoleAsync( item );
            }

            await user.GrantRoleAsync( ctx.Guild.GetRole( punishmentRoleId ) );

            string ProfilesPath = AppDomain.CurrentDomain.BaseDirectory + @$"\ServerProfiles\";
            Program.Core.ServerProfiles.First( x => x.ID == ctx.Guild.Id ).Entries.Add( NewEntry );

            File.WriteAllText( $"{ProfilesPath}{ctx.Guild.Id}.json", JsonConvert.SerializeObject( Profile, Formatting.Indented ) );

            var isolationChannel = ctx.Guild.GetChannel( channelId );

            await ctx.RespondAsync( $"Isolated {user.Mention} at channel: {isolationChannel.Mention}" +
                $", for {Convert.ToInt32(Math.Abs((NewEntry.EntryDate - NewEntry.ReleaseDate).TotalDays))} days. Removed the following roles: {string.Join( ", ", NewEntry.RemovedRoles.Select( X => X.Mention ) )} \n" +
                $"User will be released on ({NewEntry.ReleaseDate}) +- 1-10 minutes. Will the revoked roles be given back on release? {returnRoles}." );
        }

        [Command( "releaseUser" )]
        [Description( "Releases a user from isolation." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageRoles )]
        public async Task ReleaseUser ( CommandContext ctx, ulong userID )
        {
            ServerProfile Profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            bool foundEntry = false;
            IsolationEntry entryInfo = new();

            foreach (var entry in Profile.Entries)
            {
                if (entry.IsolatedUserId == userID)
                {
                    foundEntry = true;
                    entryInfo = entry;
                }
            }

            var user = ctx.Guild.GetMemberAsync( userID ).Result;

            if (!foundEntry)
            {
                await ctx.RespondAsync($"No entries found for user {user.Mention}.");
                return;
            }

            await user.RevokeRoleAsync( ctx.Guild.GetRole( entryInfo.PunishmentRoleId ) );

            if (entryInfo.ReturnRoles)
            {
                foreach (var role in entryInfo.RemovedRoles)
                {
                    await user.GrantRoleAsync( role );
                }
            }

            await ctx.RespondAsync
                (
                    $"Released user: {user.Mention} from isolation at channel: {ctx.Guild.GetChannel(entryInfo.IsolationChannelId).Mention}.\n" +
                    $"The user was isolated for {Convert.ToInt32(Math.Abs((DateTime.UtcNow - entryInfo.EntryDate).TotalDays))} days.\n" +
                    $"The isolation was called by this message {entryInfo.EntryMessageLink}.\nWere revoked roles returned? {entryInfo.ReturnRoles}.\n" +
                    $"The user was isolated for `\"{entryInfo.Reason}\"`."
                );

            string ProfilesPath = AppDomain.CurrentDomain.BaseDirectory + @$"\ServerProfiles\";
            Program.Core.ServerProfiles.First( x => x.ID == ctx.Guild.Id ).Entries.Remove(entryInfo);

            File.WriteAllText( $"{ProfilesPath}{ctx.Guild.Id}.json", JsonConvert.SerializeObject( Profile, Formatting.Indented ) );
        }

        public static async Task ReleaseEntry ( ServerProfile profile, IsolationEntry entry )
        {
            var cmds = Program.Core.Client.GetCommandsNext();
            var cmd = cmds.FindCommand( "isolate", out var customArgs );
            customArgs = "[]help. Hunting For Pulsars.";
            var guild = Program.Core.Client.GetGuildAsync( profile.ID ).Result;
            var user = guild.GetMemberAsync( entry.IsolatedUserId ).Result;
            var fakeContext = cmds.CreateFakeContext
                (
                    user,
                    Program.Core.Client.GetChannelAsync( entry.IsolationChannelId ).Result,
                    "isolate", ">",
                    cmd,
                    customArgs
                );

            if (entry.ReturnRoles)
            {
                foreach (var role in entry.RemovedRoles)
                {
                    await user.GrantRoleAsync( role );
                }
            }

            await user.RevokeRoleAsync( guild.GetRole( entry.PunishmentRoleId ) );

            await fakeContext.RespondAsync
                (
                    $"Released user: {user.Mention} from isolation at channel: {fakeContext.Guild.GetChannel( entry.IsolationChannelId ).Mention}.\n" +
                    $"The user was isolated for {Convert.ToInt32( Math.Abs( ( DateTime.UtcNow - entry.EntryDate ).TotalDays ) )} days.\n" +
                    $"The isolation was called by this message {entry.EntryMessageLink}.\nWere revoked roles returned? {entry.ReturnRoles}.\n" +
                    $"The user was isolated for `\"{entry.Reason}\"`."
                );

            string ProfilesPath = AppDomain.CurrentDomain.BaseDirectory + @$"\ServerProfiles\";
            Program.Core.ServerProfiles.First( x => x.ID == fakeContext.Guild.Id ).Entries.Remove( entry );

            File.WriteAllText( $"{ProfilesPath}{fakeContext.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }
    }
}

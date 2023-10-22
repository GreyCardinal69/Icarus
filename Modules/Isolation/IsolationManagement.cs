using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Newtonsoft.Json;

using Icarus.Modules.Profiles;

namespace Icarus.Modules.Isolation
{
    public class IsolationManagement : BaseCommandModule
    {
        [Command( "isolate" )]
        [Description( "Isolates a user in a channel with specified parameters." )]
        [Require​User​Permissions​( DSharpPlus.Permissions.ManageRoles )]
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

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );
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
                ReturnRoles = returnRoles,
                Reason = reason
            };

            var userP = JsonConvert.DeserializeObject<UserProfile>(
                File.ReadAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{userId}.json" ) );

            userP.PunishmentEntries.Add( ( DateTime.UtcNow, reason ) );

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{userId}.json",
                 JsonConvert.SerializeObject( userP, Formatting.Indented ) );

            foreach (var item in user.Roles.ToList())
            {
                await user.RevokeRoleAsync( item );
            }

            await user.GrantRoleAsync( ctx.Guild.GetRole( punishmentRoleId ) );

            string profilesPath = AppDomain.CurrentDomain.BaseDirectory + @$"\ServerProfiles\";
            profile.Entries.Add( NewEntry );

            File.WriteAllText( $"{profilesPath}{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );

            var isolationChannel = ctx.Guild.GetChannel( channelId );

            await ctx.RespondAsync( $"Isolated {user.Mention} at channel: {isolationChannel.Mention}" +
                $", for {Convert.ToInt32(Math.Abs((NewEntry.EntryDate - NewEntry.ReleaseDate).TotalDays))} days. Removed the following roles: {string.Join( ", ", NewEntry.RemovedRoles.Select( X => X.Mention ) )} \n" +
                $"User will be released on ({NewEntry.ReleaseDate}) +- 1-10 minutes. Will the revoked roles be given back on release? {returnRoles}." );
        }

        [Command( "releaseUser" )]
        [Description( "Releases a user from isolation." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageRoles )]
        public async Task ReleaseUser ( CommandContext ctx, ulong userId )
        {
            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            bool foundEntry = false;
            IsolationEntry entryInfo = new();

            foreach (var entry in profile.Entries)
            {
                if (entry.IsolatedUserId == userId)
                {
                    foundEntry = true;
                    entryInfo = entry;
                }
            }

            var user = ctx.Guild.GetMemberAsync( userId ).Result;

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
                    $"The user was isolated for {Convert.ToInt32(Math.Abs((DateTime.UtcNow - entryInfo.EntryDate).TotalDays))} days." +
                    $"\nWere revoked roles returned? {entryInfo.ReturnRoles}.\n" + $"The user was isolated for `\"{entryInfo.Reason}\"`. " +
                    $"The isolation was called by this message: " + entryInfo.EntryMessageLink
                );
   

            string profilesPath = AppDomain.CurrentDomain.BaseDirectory + @$"\ServerProfiles\";
            profile.Entries.Remove(entryInfo);

            File.WriteAllText( $"{profilesPath}{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
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
                    guild.GetChannel( profile.LogConfig.MajorNotificationsChannelId ),
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
                    $"The user was isolated for {Convert.ToInt32( Math.Abs( ( DateTime.UtcNow - entry.EntryDate ).TotalDays ) )} days.\n"
                );
            await fakeContext.RespondAsync( "The isolation was called by this message: " + entry.EntryMessageLink.ToString() );
            await fakeContext.RespondAsync( $"Were revoked roles returned? {entry.ReturnRoles}.\n" + $"The user was isolated for `\"{entry.Reason}\"`." );

            string profilesPath = AppDomain.CurrentDomain.BaseDirectory + @$"\ServerProfiles\";
            profile.Entries.Remove( entry );

            File.WriteAllText( $"{profilesPath}{fakeContext.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }
    }
}

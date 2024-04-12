using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Icarus.Modules.Profiles;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Icarus.Modules.Isolation
{
    public class IsolationManagement : BaseCommandModule
    {
        [Command( "isolate" )]
        [Description( "Isolates a user in a channel with specified parameters." )]
        [Require​User​Permissions​( DSharpPlus.Permissions.ManageRoles )]
        public async Task Isolate( CommandContext ctx, ulong punishmentRoleId, ulong userId, ulong channelId, string timeLen, bool returnRoles, [RemainingText] string reason )
        {
            await ctx.TriggerTypingAsync();

            if ( !Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ) )
            {
                await ctx.RespondAsync( "Server is not registered, call `>RegisterServer` to proceed." );
                return;
            }

            if ( ctx.Guild.GetMemberAsync( userId ) == null )
            {
                await ctx.RespondAsync( "Invalid user id." );
                return;
            }

            if ( ctx.Guild.GetRole( punishmentRoleId ) == null )
            {
                await ctx.RespondAsync( "Invalid containment role id." );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );
            DiscordMember user = ctx.Guild.GetMemberAsync( userId ).Result;

            IsolationEntry NewEntry = new IsolationEntry()
            {
                IsolationChannel = ctx.Guild.GetChannel( channelId ).Mention,
                IsolationChannelId = channelId,
                PunishmentRoleId = punishmentRoleId,
                EntryMessageLink = ctx.Message.JumpLink.ToString(),
                IsolatedUserId = userId,
                IsolatedUserName = user.DisplayName,
                EntryDate = DateTime.Now,
                ReleaseDate = timeLen.EndsWith( 'd' )
                        ? DateTime.Now.AddDays( Convert.ToDouble( timeLen[..^1] ) )
                        : DateTime.Now.AddMonths( Convert.ToInt32( timeLen[..^1] ) ),
                RemovedRoles = user.Roles.ToList(),
                ReturnRoles = returnRoles,
                Reason = reason
            };

            UserProfile userP = JsonConvert.DeserializeObject<UserProfile>(
                File.ReadAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{userId}.json" ) );

            userP.PunishmentEntries.Add( (DateTime.Now, reason) );

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{userId}.json",
                 JsonConvert.SerializeObject( userP, Formatting.Indented ) );

            foreach ( DiscordRole item in user.Roles.ToList() )
            {
                await user.RevokeRoleAsync( item );
            }

            await user.GrantRoleAsync( ctx.Guild.GetRole( punishmentRoleId ) );

            string profilesPath = AppDomain.CurrentDomain.BaseDirectory + @$"\ServerProfiles\";
            profile.Entries.Add( NewEntry );

            File.WriteAllText( $"{profilesPath}{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );

            DiscordChannel isolationChannel = ctx.Guild.GetChannel( channelId );

            await ctx.RespondAsync( $"Isolated {user.Mention} at channel: {isolationChannel.Mention}, for {Convert.ToInt32( Math.Abs( ( NewEntry.EntryDate - NewEntry.ReleaseDate ).TotalDays ) )} days. Removed the following roles: {string.Join( ", ", NewEntry.RemovedRoles.Select( X => X.Mention ) )}. \n The user will be released on: ({NewEntry.ReleaseDate}) +- 1-10 minutes. Will the revoked roles be given back on release? {returnRoles}." );
        }

        [Command( "releaseUser" )]
        [Description( "Releases a user from isolation." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageRoles )]
        public async Task ReleaseUser( CommandContext ctx, ulong userId )
        {
            await ctx.TriggerTypingAsync();

            if ( !Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ) )
            {
                await ctx.RespondAsync( "Server is not registered, call `>RegisterServer` to proceed." );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            bool foundEntry = false;
            IsolationEntry entryInfo = new IsolationEntry();

            foreach ( IsolationEntry entry in profile.Entries )
            {
                if ( entry.IsolatedUserId == userId )
                {
                    foundEntry = true;
                    entryInfo = entry;
                }
            }

            DiscordMember user = ctx.Guild.GetMemberAsync( userId ).Result;

            if ( !foundEntry )
            {
                await ctx.RespondAsync( $"No entries found for user {user.Mention}." );
                return;
            }

            await user.RevokeRoleAsync( ctx.Guild.GetRole( entryInfo.PunishmentRoleId ) );

            if ( entryInfo.ReturnRoles )
            {
                foreach ( DiscordRole role in entryInfo.RemovedRoles )
                {
                    await user.GrantRoleAsync( role );
                }
            }

            await ctx.RespondAsync
                (
                    $"Released user: {user.Mention} from isolation at channel: {ctx.Guild.GetChannel( entryInfo.IsolationChannelId ).Mention}.\n" +
                    $"The user was isolated for {Convert.ToInt32( Math.Abs( ( DateTime.Now - entryInfo.EntryDate ).TotalDays ) )} days." +
                    $"\nWere revoked roles returned? {entryInfo.ReturnRoles}.\n" + $"The user was isolated for `\"{entryInfo.Reason}\"`. " +
                    $"The isolation was called by this message: " + entryInfo.EntryMessageLink
                );

            string profilesPath = AppDomain.CurrentDomain.BaseDirectory + @$"\ServerProfiles\";
            profile.Entries.Remove( entryInfo );

            File.WriteAllText( $"{profilesPath}{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }

        public static async Task ReleaseEntry( ServerProfile profile, IsolationEntry entry )
        {
            CommandContext fakeContext = Program.Core.CreateCommandContext( profile.ID, profile.LogConfig.MajorNotificationsChannelId );
            DiscordMember user = fakeContext.Guild.GetMemberAsync( entry.IsolatedUserId ).Result;

            if ( entry.ReturnRoles )
            {
                foreach ( DiscordRole role in entry.RemovedRoles )
                {
                    await user.GrantRoleAsync( role );
                }
            }

            await user.RevokeRoleAsync( fakeContext.Guild.GetRole( entry.PunishmentRoleId ) );

            await fakeContext.RespondAsync
                (
                    $"Released user: {user.Mention} from isolation at channel: {fakeContext.Guild.GetChannel( entry.IsolationChannelId ).Mention}.\n The user was isolated for {Convert.ToInt32( Math.Abs( ( DateTime.Now - entry.EntryDate ).TotalDays ) )} days.\n"
                );
            await fakeContext.RespondAsync( $"The isolation was called by this message: {entry.EntryMessageLink}" );
            await fakeContext.RespondAsync( $"Were revoked roles returned? {entry.ReturnRoles}.\n The user was isolated for `\"{entry.Reason}\"`." );

            string profilesPath = $"{AppDomain.CurrentDomain.BaseDirectory}\\ServerProfiles\\";
            profile.Entries.Remove( entry );

            File.WriteAllText( $"{profilesPath}{fakeContext.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }
    }
}
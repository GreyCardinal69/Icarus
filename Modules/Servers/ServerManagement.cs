using System;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using System.IO;

using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Entities;

using Newtonsoft.Json;

namespace Icarus.Modules.Servers
{
    public class ServerManagement : BaseCommandModule
    {
        [Command( "registerProfile" )]
        [Description( "Creates a server profile for the server where executed." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.Administrator )]
        public async Task RegisterServer ( CommandContext ctx, bool overWrite = false )
        {
            await ctx.TriggerTypingAsync();
            string profilesPath = AppDomain.CurrentDomain.BaseDirectory + @$"\ServerProfiles\";

            if (File.Exists( $"{profilesPath}{ctx.Guild.Id}.json" ) && !overWrite)
            {
                await ctx.RespondAsync( $"A server profile for this server already exists, do you want to overwrite it ? If yes type `>registerserver true`" );
                return;
            }

            ServerProfile Profile = new()
            {
                Name = ctx.Guild.Name,
                ID = ctx.Guild.Id,
                ProfileCreationDate = DateTime.UtcNow,
                WordBlackList = new(),
                AntiSpam = new(),
                AntiSpamIgnored = new(),
                Entries = new(),
                LogConfig = new(),
            };

            Program.Core.RegisteredServerIds.Add( ctx.Guild.Id );
            Program.Core.ServerProfiles.Add( Profile );

            File.WriteAllText( $"{profilesPath}{ctx.Guild.Id}.json", JsonConvert.SerializeObject( Profile, Formatting.Indented ) );
            await ctx.RespondAsync( $"Created a new server profile for {ctx.Guild.Name}." );
        }

        [Command( "confAntiSpam" )]
        [Description( "Changes server anti spam module configurations." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task EnableLogging ( CommandContext ctx, int first, int second, int third, int limit )
        {
            await ctx.TriggerTypingAsync();

            if (!Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ))
            {
                await ctx.RespondAsync( "Server is not registered, can not change anti spam configurations." );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            profile.AntiSpam = new()
            {
                FirstWarning = first,
                SecondWarning = second,
                LastWarning = third,
                Limit = limit,
                CacheResetInterval = 20
            };

            await ctx.RespondAsync(
                $"Changed anti spam configurations. Message cache is reset every 20 seconds. First warning is issued if a user sends {first} messages " +
                $"in that interval, second: {second}, last: {third}. At {limit} or more the user is isolated."
            );
            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }

        [Command( "antiSpamIgnore" )]
        [Description( "Tells the anti spam module to ignore the specified channels." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageChannels )]
        public async Task EnableLogging ( CommandContext ctx, params ulong[] channels )
        {
            await ctx.TriggerTypingAsync();

            if (!Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ))
            {
                await ctx.RespondAsync( "Server is not registered, can not change anti spam configurations." );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );
            DiscordChannel[] mentions = new DiscordChannel[channels.Length];

            int i = 0;
            foreach (var item in channels)
            {
                mentions[i] = ctx.Guild.GetChannel( item );
                profile.AntiSpamIgnored.Add( item );
                i++;
            }

            await ctx.RespondAsync(
                $"Configured anti spam module to ignore the following channels: {string.Join(", ", mentions.Select( x => x.Mention ) ) }."
            );
            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }

        [Command( "antiSpamReset" )]
        [Description( "Resets anti spam module ignored channels" )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageChannels )]
        public async Task ResetAntiSpamIgnored ( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();

            if (!Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ))
            {
                await ctx.RespondAsync( "Server is not registered, can not change anti spam configurations." );
                return;
            }

            var profile = ServerProfile.ProfileFromId( ctx.Guild.Id );
            profile.AntiSpamIgnored.Clear();

            await ctx.RespondAsync($"The anti spam module no longer ignores any channels.");
            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }

        [Command( "deleteProfile" )]
        [Description( "Deletes the server profile of the server." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.Administrator )]
        public async Task RegisterServer ( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            string profilesPath = AppDomain.CurrentDomain.BaseDirectory + @$"\ServerProfiles\";

            await ctx.RespondAsync( "Confirm action by responding with \"yes\" " );

            var interactivity = ctx.Client.GetInteractivity();
            var msg = await interactivity.WaitForMessageAsync
            (
                xm => string.Equals(xm.Content, "yes",
                StringComparison.InvariantCultureIgnoreCase),
                TimeSpan.FromSeconds( 15 )
            );

            if (!msg.TimedOut && msg.Result.Author.Id == ctx.User.Id )
            {
                File.Delete( $"{profilesPath}{ctx.Guild.Id}.json" );
                Program.Core.ServerProfiles.Remove( ServerProfile.ProfileFromId( ctx.Guild.Id ) );
                Program.Core.RegisteredServerIds.Remove( ctx.Guild.Id );
                await ctx.RespondAsync( $"Deleted the server profile for {ctx.Guild.Name}." );
            }
            else
            {
                await ctx.RespondAsync( "Confirmation time ran out, aborting." );
            }
        }

        [Command( "profile" )]
        [Description( "Responds with information on the server profile." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageChannels )]
        public async Task Profile ( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();

            if (!Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ))
            {
                await ctx.RespondAsync( "Server is not registered, can not provide information." );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            StringBuilder enabledEvents = new();

            if (profile.LogConfig.GuildMemberRemoved)
                enabledEvents.Append( "GuildMemberRemoved, " );
            if (profile.LogConfig.GuildMemberAdded)
                enabledEvents.Append( "GuildMemberAdded, " );
            if (profile.LogConfig.GuildBanRemoved)
                enabledEvents.Append( "GuildBanRemoved, " );
            if (profile.LogConfig.GuildBanAdded)
                enabledEvents.Append( "GuildBanAdded, " );
            if (profile.LogConfig.GuildRoleCreated)
                enabledEvents.Append( "GuildRoleCreated, " );
            if (profile.LogConfig.GuildRoleUpdated)
                enabledEvents.Append( "GuildRoleUpdated, " );
            if (profile.LogConfig.GuildRoleDeleted)
                enabledEvents.Append( "GuildRoleDeleted, " );
            if (profile.LogConfig.MessageReactionsCleared)
                enabledEvents.Append( "MessageReactionsCleared, " );
            if (profile.LogConfig.MessageReactionRemoved)
                enabledEvents.Append( "MessageReactionRemoved, " );
            if (profile.LogConfig.MessageReactionAdded)
                enabledEvents.Append( "MessageReactionAdded, " );
            if (profile.LogConfig.MessagesBulkDeleted)
                enabledEvents.Append( "MessagesBulkDeleted, " );
            if (profile.LogConfig.MessageCreated)
                enabledEvents.Append( "MessageCreated, " );
            if (profile.LogConfig.MessageDeleted)
                enabledEvents.Append( "MessageDeleted, " );
            if (profile.LogConfig.MessageUpdated)
                enabledEvents.Append( "MessageUpdated, " );
            if (profile.LogConfig.InviteCreated)
                enabledEvents.Append( "InviteCreated, " );
            if (profile.LogConfig.InviteDeleted)
                enabledEvents.Append( "InviteDeleted, " );
            if (profile.LogConfig.ChannelCreated)
                enabledEvents.Append( "ChannelCreated, " );
            if (profile.LogConfig.ChannelDeleted)
                enabledEvents.Append( "ChannelDeleted, " );
            if (profile.LogConfig.ChannelUpdated)
                enabledEvents.Append( "ChannelUpdated." );

            string[] mentions = new string[profile.AntiSpamIgnored.Count];

            int i = 0;
            foreach (var item in profile.AntiSpamIgnored)
            {
                mentions[i] = ctx.Guild.GetChannel( item ).Mention;
                i++;
            }

            var ignores = string.Join( ", ", mentions );

            var defChannel = ctx.Guild.GetChannel( profile.LogConfig.LogChannel );
            var majorNotifChannel = ctx.Guild.GetChannel( profile.LogConfig.MajorNotificationsChannelId );
            var defaultContainmentChannel = ctx.Guild.GetChannel( profile.LogConfig.DefaultContainmentChannelId );
            var containmentRole = ctx.Guild.GetRole( profile.LogConfig.DefaultContainmentRoleId );

            var embed = new DiscordEmbedBuilder
            {
                Title = $"Server Profile for {ctx.Guild.Name}",
                Color = DiscordColor.SpringGreen,
                Description =
                    $"Logging Enabled?: {profile.LogConfig.LoggingEnabled}.\n\n" +
                    $"Logging enabled for following events: {(enabledEvents.Length == 0 ? "NONE" : enabledEvents)}\n\n" +
                    $"Default notifications are sent to: {( defChannel == null ? "NONE" : defChannel.Mention)}.\n\n" +
                    $"Major notifications are sent to: {(majorNotifChannel == null ? "NONE" :majorNotifChannel.Mention)}.\n\n" +
                    $"The default containment channel is: {( defaultContainmentChannel  == null ? "NONE" : defaultContainmentChannel.Mention)}.\n\n" +
                    $"The default containment role is: {( containmentRole  == null ? "NONE" : containmentRole.Mention)}.\n\n" +
                    $"The server contains {profile.Entries.Count} active isolation entries.\n\n" +
                    $"Anti spam is configured at {profile.AntiSpam.FirstWarning}, {profile.AntiSpam.SecondWarning}, {profile.AntiSpam.LastWarning}, {profile.AntiSpam.Limit} " +
                    $"messages per 20 seconds. The following channels are exempt from anti spam module: {(ignores.Length == 0 ? "None" : ignores)}.\n\n" +
                    $"The following words are black-listed and users mentioning them will be reported: {string.Join(", ", profile.WordBlackList)}.\n\n" +
                    $"Server profile created at: {profile.ProfileCreationDate}.",
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
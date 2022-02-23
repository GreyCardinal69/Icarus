using System;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;

using Newtonsoft.Json;
using System.IO;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Entities;
using System.Text;
using System.Linq;

namespace Icarus.Modules.Servers
{
    public class ServerManagement : BaseCommandModule
    {
        [Command( "registerProfile" )]
        [Description( "Creates a server profile for the server where executed." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.Administrator )]
        public async Task RegisterServer ( CommandContext ctx, bool OverWrite = false )
        {
            await ctx.TriggerTypingAsync();
            string ProfilesPath = AppDomain.CurrentDomain.BaseDirectory + @$"\ServerProfiles\";

            if (File.Exists( $"{ProfilesPath}{ctx.Guild.Id}.json" ) && !OverWrite)
            {
                await ctx.RespondAsync( $"A server profile for this server already exists, do you want to overwrite it ? If yes type `>registerserver true`" );
                return;
            }

            ServerProfile Profile = new()
            {
                Name = ctx.Guild.Name,
                ID = ctx.Guild.Id,
                ProfileCreationDate = DateTime.UtcNow
            };

            File.WriteAllText( $"{ProfilesPath}{ctx.Guild.Id}.json", JsonConvert.SerializeObject( Profile, Formatting.Indented ) );
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

            ServerProfile Profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            Program.Core.ServerProfiles.First( x => x.ID == ctx.Guild.Id ).AntiSpam = new()
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
            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( Profile, Formatting.Indented ) );
        }

        [Command( "deleteProfile" )]
        [Description( "Creates a server profile for the server where executed." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.Administrator )]
        public async Task RegisterServer ( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            string ProfilesPath = AppDomain.CurrentDomain.BaseDirectory + @$"\ServerProfiles\";

            await ctx.RespondAsync( " Confirm action by responding with \"yes\" " );

            var interactivity = ctx.Client.GetInteractivity();
            var msg = await interactivity.WaitForMessageAsync
            (
                xm => string.Equals(xm.Content, "yes",
                StringComparison.InvariantCultureIgnoreCase),
                TimeSpan.FromSeconds( 60 )
            );

            if (!msg.TimedOut)
            {
                File.Delete( $"{ProfilesPath}{ctx.Guild.Id}.json" );
                await ctx.RespondAsync( $"Deleted the server profile for {ctx.Guild.Name}." );
            }
            else
            {
                await ctx.RespondAsync( "Confirmation time ran out, aborting." );
            }
        }

        [Command( "profile" )]
        [Description( "Responds with information on the server profile." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageRoles )]
        public async Task HelpBasic ( CommandContext ctx )
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

            var embed = new DiscordEmbedBuilder
            {
                Title = $"Server Profile for {ctx.Guild.Name}",
                Color = DiscordColor.SpringGreen,
                Description =
                    $"Logging Enabled?: {profile.LogConfig.LoggingEnabled}.\n\n" +
                    $"Logging enabled for following events: {enabledEvents}\n\n" +
                    $"Default notifications are sent to: {ctx.Guild.GetChannel(profile.LogConfig.LogChannel).Mention}.\n\n" +
                    $"Major notifications are sent to: {ctx.Guild.GetChannel( profile.LogConfig.MajorNotificationsChannelId ).Mention}.\n\n" +
                    $"The default containment channel is: {ctx.Guild.GetChannel( profile.LogConfig.DefaultContainmentChannelId ).Mention}.\n\n" +
                    $"The default containment role is: {ctx.Guild.GetRole(profile.LogConfig.DefaultContainmentRoleId).Mention}.\n\n" +
                    $"The server contains {profile.Entries.Count} active isolation entries.\n\n" +
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
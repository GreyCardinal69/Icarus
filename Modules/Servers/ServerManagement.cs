using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Icarus.Modules.Isolation;
using Icarus.Modules.Profiles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Modules.Servers
{
    public class ServerManagement : BaseCommandModule
    {
        [Command( "RegisterProfile" )]
        [Description( "Creates a server profile for the server where executed." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.Administrator )]
        public async Task RegisterServer( CommandContext ctx, bool overWrite = false )
        {
            await ctx.TriggerTypingAsync();
            string profilesPath = AppDomain.CurrentDomain.BaseDirectory + @$"\ServerProfiles\";

            if ( File.Exists( $"{profilesPath}{ctx.Guild.Id}.json" ) && !overWrite )
            {
                await ctx.RespondAsync( $"A server profile for this server already exists, do you want to overwrite it ? If yes type `>registerserver true`" );
                return;
            }

            ServerProfile Profile = new()
            {
                Name = ctx.Guild.Name,
                ID = ctx.Guild.Id,
                ProfileCreationDate = DateTime.Now,
                WordBlackList = new List<string>(),
                AntiSpam = new AntiSpamProfile(),
                AntiSpamIgnored = new List<ulong>(),
                Entries = new List<IsolationEntry>(),
                LogConfig = new LogProfile(),
            };

            Program.Core.RegisteredServerIds.Add( ctx.Guild.Id );
            Program.Core.ServerProfiles.Add( Profile );

            File.WriteAllText( $"{profilesPath}{ctx.Guild.Id}.json", JsonConvert.SerializeObject( Profile, Formatting.Indented ) );
            await ctx.RespondAsync( $"Created a new server profile for {ctx.Guild.Name}." );
        }

        [Command( "ConfAntiSpam" )]
        [Description( "Changes server anti spam module configurations." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task EnableLogging( CommandContext ctx, int first, int second, int third, int limit )
        {
            await ctx.TriggerTypingAsync();

            if ( !Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ) )
            {
                await ctx.RespondAsync( "Server is not registered, can not change anti spam configurations." );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            profile.AntiSpam = new AntiSpamProfile()
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

        [Command( "AntiSpamIgnore" )]
        [Description( "Tells the anti spam module to ignore the specified channels." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageChannels )]
        public async Task EnableLogging( CommandContext ctx, params ulong[] channels )
        {
            await ctx.TriggerTypingAsync();

            if ( !Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ) )
            {
                await ctx.RespondAsync( "Server is not registered, can not change anti spam configurations." );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );
            DiscordChannel[] mentions = new DiscordChannel[channels.Length];

            int i = 0;
            foreach ( ulong item in channels )
            {
                mentions[i] = ctx.Guild.GetChannel( item );
                profile.AntiSpamIgnored.Add( item );
                i++;
            }

            await ctx.RespondAsync(
                $"Configured anti spam module to ignore the following channels: {string.Join( ", ", mentions.Select( x => x.Mention ) )}."
            );
            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }

        [Command( "UpdateServerFields" )]
        [Description( "Adds new and or removes old server profiles data fields." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageChannels )]
        public async Task UpdateServerFields( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();

            if ( !Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ) )
            {
                await ctx.RespondAsync( "Server is not registered, aborting." );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            await ctx.RespondAsync( $"Updated server profile fields." );
            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }

        [Command( "AntiSpamReset" )]
        [Description( "Resets anti spam module ignored channels" )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageChannels )]
        public async Task ResetAntiSpamIgnored( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();

            if ( !Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ) )
            {
                await ctx.RespondAsync( "Server is not registered, can not change anti spam configurations." );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );
            profile.AntiSpamIgnored.Clear();

            await ctx.RespondAsync( $"The anti spam module no longer ignores any channels." );
            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }

        [Command( "DisableCustomWelcome" )]
        [Description( "Disables the custom welcome message for the server." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task DisableCustomWelcome( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();

            if ( !Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ) )
            {
                await ctx.RespondAsync( "Server is not registered, aborting." );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            if ( !profile.HasCustomWelcome )
            {
                await ctx.RespondAsync("Server does not have a set custom welcome message.");
                return;
            }

            await ctx.RespondAsync( "Custom welcome message removed." );

            profile.RemoveCustomWelcome();

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }

        [Command( "SetCustomWelcome" )]
        [Description( "Sets a custom welcome message that the bot will execute for new users." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task SetCustomWelcome( CommandContext ctx, string message, ulong roleId, ulong channelId )
        {
            await ctx.TriggerTypingAsync();

            if ( !Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ) )
            {
                await ctx.RespondAsync( "Server is not registered, aborting." );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            if ( message == "" || channelId == 0 )
            {
                await ctx.RespondAsync( "Invalid message or channelId." );
                return;
            }

            await ctx.RespondAsync( "Custom welcome message set!" );

            profile.SetCustomWelcome( new UserWelcome()
            {
                ChannelId = channelId,
                Message = message,
                RoleId = roleId,
            } );

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }

        [Command( "AddTimedReminder" )]
        [Description( "Adds a timed reminder for the server." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task AddTimedReminder( CommandContext ctx, string name, string content, bool repeat, string dateType, string date )
        {
            await ctx.TriggerTypingAsync();

            if ( !Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ) )
            {
                await ctx.RespondAsync( "Server is not registered, aborting." );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            name = name.Replace( '_', ' ' );

            for ( int i = 0; i < profile.TimedReminders.Count; i++ )
            {
                if ( string.Equals(name, profile.TimedReminders[i].Name ) )
                {
                    await ctx.RespondAsync( $"Timed reminder with ID: \"{name}\" already exists." );
                    return;
                }
            }

            TimedReminder reminder = new TimedReminder( name.Replace( '_', ' ' ), content.Replace( '_', ' ' ), repeat, dateType, date );

            profile.TimedReminders.Add( reminder );
            await ctx.RespondAsync( $"Timed Reminder: `{name}` successfully added.\nThe reminder will go off at: <t:{reminder.ExpDate}>." );
            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }

        [Command( "ListTimedReminders" )]
        [Description( "Adds a timed reminder for the server." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task ListTimedReminders( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();

            if ( !Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ) )
            {
                await ctx.RespondAsync( "Server is not registered, aborting." );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            if ( profile.TimedReminders.Count <= 0 )
            {
                await ctx.RespondAsync("No timed reminders registered.");
                return;
            }

            StringBuilder sb = new StringBuilder("```");

            int i = 1;
            foreach ( TimedReminder item in profile.TimedReminders )
            {
                sb.Append($"Reminder #{i}:\nName ( ID format ): \t{item.Name.Replace(' ','_')}\nContent: \t{item.Content}\nThe Reminder will go off at: \t<t:{item.ExpDate}>\n\n");
                i++;
            }
            sb.Append( "```" );
            await ctx.RespondAsync( sb.ToString() );
        }


        [Command( "RemoveTimedReminder" )]
        [Description( "Adds a timed reminder for the server." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task RemoveTimedReminder( CommandContext ctx, string name )
        {
            await ctx.TriggerTypingAsync();

            if ( !Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ) )
            {
                await ctx.RespondAsync( "Server is not registered, aborting." );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );
            bool removed = false;

            var clean = name.Replace( '_', ' ' );

            for ( int i = 0; i < profile.TimedReminders.Count; i++ )
            {
                if ( profile.TimedReminders[i].Name == clean )
                {
                    profile.TimedReminders.RemoveAt( i );
                    await ctx.RespondAsync( $"Timed Reminder: `{clean}` successfully removed." );
                    removed = true;
                    File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
                }
            }
            if ( !removed ) await ctx.RespondAsync( $"Timed reminder with ID: {clean} not found." );
        }

        [Command( "DeleteProfile" )]
        [Description( "Deletes the server profile of the server." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.Administrator )]
        public async Task RegisterServer ( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            string profilesPath = AppDomain.CurrentDomain.BaseDirectory + @$"\ServerProfiles\";

            await ctx.RespondAsync( "Confirm action by responding with \"yes\" " );

            InteractivityExtension interactivity = ctx.Client.GetInteractivity();
            InteractivityResult<DiscordMessage> msg = await interactivity.WaitForMessageAsync
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

        [Command( "Profile" )]
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

            List<string> enabledEvents = new List<string>();

            if (profile.LogConfig.GuildMemberRemoved)
                enabledEvents.Add( "GuildMemberRemoved" );
            if (profile.LogConfig.GuildMemberAdded)
                enabledEvents.Add( "GuildMemberAdded" );
            if (profile.LogConfig.GuildBanRemoved)
                enabledEvents.Add( "GuildBanRemoved" );
            if (profile.LogConfig.GuildBanAdded)
                enabledEvents.Add( "GuildBanAdde" );
            if (profile.LogConfig.GuildRoleCreated)
                enabledEvents.Add( "GuildRoleCreate " );
            if (profile.LogConfig.GuildRoleUpdated)
                enabledEvents.Add( "GuildRoleUpdated" );
            if (profile.LogConfig.GuildRoleDeleted)
                enabledEvents.Add( "GuildRoleDeleted" );
            if (profile.LogConfig.MessageReactionsCleared)
                enabledEvents.Add( "MessageReactionsCleared" );
            if (profile.LogConfig.MessageReactionRemoved)
                enabledEvents.Add( "MessageReactionRemoved" );
            if (profile.LogConfig.MessageReactionAdded)
                enabledEvents.Add( "MessageReactionAdded" );
            if (profile.LogConfig.MessagesBulkDeleted)
                enabledEvents.Add( "MessagesBulkDeleted" );
            if (profile.LogConfig.MessageCreated)
                enabledEvents.Add( "MessageCreated" );
            if (profile.LogConfig.MessageDeleted)
                enabledEvents.Add( "MessageDeleted" );
            if (profile.LogConfig.MessageUpdated)
                enabledEvents.Add( "MessageUpdated" );
            if (profile.LogConfig.InviteCreated)
                enabledEvents.Add( "InviteCreated" );
            if (profile.LogConfig.InviteDeleted)
                enabledEvents.Add( "InviteDeleted" );
            if (profile.LogConfig.ChannelCreated)
                enabledEvents.Add( "ChannelCreated" );
            if (profile.LogConfig.ChannelDeleted)
                enabledEvents.Add( "ChannelDeleted" );
            if (profile.LogConfig.ChannelUpdated)
                enabledEvents.Add( "ChannelUpdated" );

            string[] mentions = new string[profile.AntiSpamIgnored.Count];

            int i = 0;
            foreach (ulong item in profile.AntiSpamIgnored)
            {
                mentions[i] = ctx.Guild.GetChannel( item ).Mention;
                i++;
            }

            string ignores = string.Join( ", ", mentions );

            DiscordChannel defChannel = ctx.Guild.GetChannel( profile.LogConfig.LogChannel );
            DiscordChannel majorNotifChannel = ctx.Guild.GetChannel( profile.LogConfig.MajorNotificationsChannelId );
            DiscordChannel defaultContainmentChannel = ctx.Guild.GetChannel( profile.LogConfig.DefaultContainmentChannelId );
            DiscordRole containmentRole = ctx.Guild.GetRole( profile.LogConfig.DefaultContainmentRoleId );

            StringBuilder timedReminders = new StringBuilder("```");

            foreach ( TimedReminder item in profile.TimedReminders )
            {
                timedReminders.Append( $"{item.Name}: Will go off at: {DateTimeOffset.FromUnixTimeSeconds(item.ExpDate)} / <t:{item.ExpDate}> in Unix.\n\n" );
            }
            timedReminders.Append( "```" );
            Console.WriteLine(profile.HasCustomWelcome);
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder
            {
                Title = $"Server Profile for {ctx.Guild.Name}",
                Color = DiscordColor.SpringGreen,
                Description =
                    $"Logging Enabled?: `{profile.LogConfig.LoggingEnabled}`.\n\n" +
                    $"Logging enabled for following events: ```{(enabledEvents.Count == 0 ? "NONE" : string.Join(", ", enabledEvents))}.```\n" +
                    $"Default notifications are sent to: {( defChannel == null ? "NONE" : defChannel.Mention)}.\n\n" +
                    $"Major notifications are sent to: {(majorNotifChannel == null ? "NONE" :majorNotifChannel.Mention)}.\n\n" +
                    $"The default containment channel is: {( defaultContainmentChannel  == null ? "NONE" : defaultContainmentChannel.Mention)}.\n\n" +
                    $"The default containment role is: {( containmentRole  == null ? "NONE" : containmentRole.Mention)}.\n\n" +
                    $"The server contains `{profile.Entries.Count}` active isolation entries.\n\n" +
                    $"Anti spam is configured at `{profile.AntiSpam.FirstWarning}, {profile.AntiSpam.SecondWarning}, {profile.AntiSpam.LastWarning}, {profile.AntiSpam.Limit}` " +
                    $"messages per 20 seconds. The following channels are exempt from anti spam module: {(ignores.Length == 0 ? "None" : ignores)}.\n\n" +
                    $"The following words are black-listed and users mentioning them will be reported: ```{string.Join(", ", profile.WordBlackList)}.```\n" +
                    $"The server has the following Timed Reminders queued:\n {(profile.TimedReminders.Count > 0 ? timedReminders : "None")}\n" +
                    $"The server has the following custom user welcome system set:\n {(profile.HasCustomWelcome ? $"\tMessage: {profile.CustomWelcome.Message}\n \tThe following role will be granted: {(profile.CustomWelcome.RoleId == 0 ? "None" : ctx.Guild.GetRole(profile.CustomWelcome.RoleId).Mention)}.\n\tIn the following channel: {ctx.Guild.GetChannel(profile.CustomWelcome.ChannelId).Mention}." : "`Not Set`.")}\n\n" +
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
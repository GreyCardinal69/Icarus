﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Modules.Logs
{
    public class LogManagement : BaseCommandModule
    {
        [Command( "EnableLogging" )]
        [Description( "Enables logging for the server executed in, logs go into the specified channel." )]
        [Require​User​Permissions​( DSharpPlus.Permissions.ManageChannels )]
        public async Task EnableLogging( CommandContext ctx, ulong channelId )
        {
            await ctx.TriggerTypingAsync();

            if ( !Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ) )
            {
                await ctx.RespondAsync( "Server is not registered, can not enable logging." );
                return;
            }

            if ( !ctx.Guild.Channels.ContainsKey( channelId ) )
            {
                await ctx.RespondAsync( $"Invalid channel Id: {channelId}" );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            profile.LogConfig.LogChannel = channelId;
            profile.LogConfig.ToggleLogging( true );
            await ctx.RespondAsync( $"Enabled logging for {ctx.Guild.Name} in: {ctx.Guild.GetChannel( channelId ).Mention}." );

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }

        [Command( "RemoveLogExclusion" )]
        [Description( "Enables logging for a specific channel." )]
        [Require​User​Permissions​( DSharpPlus.Permissions.ManageChannels )]
        public async Task RemoveLogExclusion( CommandContext ctx, ulong channelId )
        {
            await ctx.TriggerTypingAsync();

            if ( !Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ) )
            {
                await ctx.RespondAsync( "Server is not registered, can not enable logging." );
                return;
            }

            if ( !ctx.Guild.Channels.ContainsKey( channelId ) )
            {
                await ctx.RespondAsync( $"Invalid channel Id: {channelId}" );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            if ( profile.LogConfig.ExcludedChannels.Contains( channelId ) )
            {
                profile.LogConfig.ExcludedChannels.Remove( channelId );
            }
            else
            {
                await ctx.RespondAsync( $"Channel not excluded." );
                return;
            }

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );

            await ctx.RespondAsync( $"The channel is now logged." );
        }

        [Command( "AddLogExclusion" )]
        [Description( "Disables logging for a specific channel." )]
        [Require​User​Permissions​( DSharpPlus.Permissions.ManageChannels )]
        public async Task AddLogExclusion( CommandContext ctx, ulong channelId )
        {
            await ctx.TriggerTypingAsync();

            if ( !Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ) )
            {
                await ctx.RespondAsync( "Server is not registered, can not enable logging." );
                return;
            }

            if ( !ctx.Guild.Channels.ContainsKey( channelId ) )
            {
                await ctx.RespondAsync( $"Invalid channel Id: {channelId}" );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            if ( !profile.LogConfig.ExcludedChannels.Contains( channelId ) )
            {
                profile.LogConfig.ExcludedChannels.Add( channelId );
            }
            else
            {
                await ctx.RespondAsync( $"Channel already excluded." );
                return;
            }

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );

            await ctx.RespondAsync( $"The channel is now excluded from logging." );
        }

        [Command( "SetMajorLogChannel" )]
        [Description( "Sets the channel for major notifications." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageChannels )]
        public async Task SetMajorNotificationsChannel ( CommandContext ctx, ulong channelId )
        {
            await ctx.TriggerTypingAsync();

            if (!Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ))
            {
                await ctx.RespondAsync( "Server is not registered, can not enable logging." );
                return;
            }

            if (!ctx.Guild.Channels.ContainsKey( channelId ))
            {
                await ctx.RespondAsync( $"Invalid channel Id: {channelId}" );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            profile.LogConfig.MajorNotificationsChannelId = channelId;
            await ctx.RespondAsync( $"Set channel for important notifications of {ctx.Guild.Name} at: {ctx.Guild.GetChannel( channelId ).Mention}." );

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }

        [Command( "AddWordsBl" )]
        [Description( "Adds words to the word blacklist." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task AddWordBlacklist ( CommandContext ctx, params string[] words )
        {
            await ctx.TriggerTypingAsync();

            if (!Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ))
            {
                await ctx.RespondAsync( "Server is not registered, can not enable logging." );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            for (int i = 0; i < words.Length; i++)
            {
                if ( !profile.WordBlackList.Contains( words[i] ) )
                {
                    profile.WordBlackList.Add( words[i] );
                }
            }

            await ctx.RespondAsync(
                $"Added the following words to the server's word blacklist, any mentions of those will be reported to the major notifications channel: " +
                $"{string.Join(", ", words)}."
            );

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }

        [Command( "RemoveWordsBL" )]
        [Description( "Removes the specified words from the server's black-listed words." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task RemoveWordBlacklist ( CommandContext ctx, params string[] words )
        {
            await ctx.TriggerTypingAsync();

            if (!Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ))
            {
                await ctx.RespondAsync( "Server is not registered, can not enable logging." );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            for (int i = 0; i < words.Length; i++)
            {
                profile.WordBlackList.Remove( words[i] );
                words[i] = $"\"{words[i]}\"";
            }

            await ctx.RespondAsync(
                $"Removed the following words from the server's word blacklist: {string.Join( ", ", words )}."
            );

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }

        [Command( "SetContainmentDefaults" )]
        [Description( "Sets default containment channel and role id-s." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.Administrator )]
        public async Task SetDefaultContainmentIds ( CommandContext ctx, ulong channelId, ulong roleId )
        {
            await ctx.TriggerTypingAsync();

            if (!Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ))
            {
                await ctx.RespondAsync( "Server is not registered, can not enable logging." );
                return;
            }

            if (!ctx.Guild.Channels.ContainsKey( channelId ))
            {
                await ctx.RespondAsync( $"Invalid channel Id: {channelId}" );
                return;
            }

            if (!ctx.Guild.Roles.ContainsKey( roleId ))
            {
                await ctx.RespondAsync( $"Invalid role Id: {roleId}" );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            profile.SetContainmentDefaults( channelId, roleId );
            await ctx.RespondAsync(
                $"Set channel for default containment of {ctx.Guild.Name} at: {ctx.Guild.GetChannel( channelId ).Mention}.\n" +
                $"Default role for containment is set as {ctx.Guild.Roles[roleId].Mention}."
            );

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }

        [Command( "DisableLogging" )]
        [Description( "Disables logging for the server executed in, logs go into the specified channel." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.Administrator )]
        public async Task DisableLogging ( CommandContext ctx, ulong channelId )
        {
            await ctx.TriggerTypingAsync();

            if (!Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ))
            {
                await ctx.RespondAsync( "Server is not registered, can not disable logging." );
                return;
            }

            if (!ctx.Guild.Channels.ContainsKey( channelId ))
            {
                await ctx.RespondAsync( $"Invalid channel Id: {channelId}" );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );
            profile.LogConfig.LogChannel = channelId;

            profile.LogConfig.ToggleLogging( false );
            await ctx.RespondAsync( $"Disabled logging for {ctx.Guild.Name} in: {ctx.Guild.GetChannel( channelId )}" );

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }

        [Command( "ListLogExclusions" )]
        [Description( "Responds with channels not logged." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task ListLogExclusions( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            StringBuilder str = new();

            foreach ( var item in profile.LogConfig.ExcludedChannels )
            {
                var channel = ctx.Guild.GetChannel( item );
                str.Append( $"{channel.Name}: {channel.Id}.\n" );
            }

            await ctx.RespondAsync( str.ToString() );
        }

        [Command( "LogEvents" )]
        [Description( "Responds with available log events." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task LogEvents ( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync(
                "Listing log events: GuildMemberRemoved, GuildMemberAdded, GuildBanRemoved, GuildBanAdded, GuildRoleCreated, GuildRoleUpdated, GuildRoleDeleted, " +
                "MessageReactionsCleared, MessageReactionRemoved, MessageReactionAdded, MessagesBulkDeleted, MessageDeleted, MessageUpdated, " +
                "InviteDeleted, InviteCreated, ChannelUpdated, ChannelDeleted, ChannelCreated."
            );
        }

        [Command( "ToggleLogEvents" )]
        [Description( "Toggles log events for the server executed in, invalid events will be ignored." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.Administrator )]
        public async Task ToggleLogEvents( CommandContext ctx, params string[] eventTypes )
        {
            await ctx.TriggerTypingAsync();

            if (eventTypes.Length < 1)
            {
                await ctx.RespondAsync( "Specify at least one log event to toggle on/off." );
                return;
            }

            if (!Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ))
            {
                await ctx.RespondAsync( "Server is not registered, can not toggle log events." );
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            foreach ( string Event in eventTypes )
            {
                string str = Event.ToLower();
                switch ( str )
                {
                    case "guildmemberremoved":
                        profile.LogConfig.GuildMemberRemoved = !profile.LogConfig.GuildMemberRemoved;
                        break;
                    case "guildmemberadded":
                        profile.LogConfig.GuildMemberAdded = !profile.LogConfig.GuildMemberAdded;
                        break;
                    case "guildbanremoved":
                        profile.LogConfig.GuildBanRemoved = !profile.LogConfig.GuildBanRemoved;
                        break;
                    case "guildbanadded":
                        profile.LogConfig.GuildBanAdded = !profile.LogConfig.GuildBanAdded;
                        break;
                    case "guildrolecreated":
                        profile.LogConfig.GuildRoleCreated = !profile.LogConfig.GuildRoleCreated;
                        break;
                    case "guildroleupdated":
                        profile.LogConfig.GuildRoleUpdated = !profile.LogConfig.GuildRoleUpdated;
                        break;
                    case "guildroledeleted":
                        profile.LogConfig.GuildRoleDeleted = !profile.LogConfig.GuildRoleDeleted;
                        break;
                    case "messagereactionscleared":
                        profile.LogConfig.MessageReactionsCleared = !profile.LogConfig.MessageReactionsCleared;
                        break;
                    case "messagereactionremoved":
                        profile.LogConfig.MessageReactionRemoved = !profile.LogConfig.MessageReactionRemoved;
                        break;
                    case "messagereactionadded":
                        profile.LogConfig.MessageReactionAdded = !profile.LogConfig.MessageReactionAdded;
                        break;
                    case "messagesbulkdeleted":
                        profile.LogConfig.MessagesBulkDeleted = !profile.LogConfig.MessagesBulkDeleted;
                        break;
                    case "messagedeleted":
                        profile.LogConfig.MessageDeleted = !profile.LogConfig.MessageDeleted;
                        break;
                    case "messageupdated":
                        profile.LogConfig.MessageUpdated = !profile.LogConfig.MessageUpdated;
                        break;
                    case "messagecreated":
                        profile.LogConfig.MessageCreated = !profile.LogConfig.MessageCreated;
                        break;
                    case "invitedeleted":
                        profile.LogConfig.InviteDeleted = !profile.LogConfig.InviteDeleted;
                        break;
                    case "invitecreated":
                        profile.LogConfig.InviteCreated = !profile.LogConfig.InviteCreated;
                        break;
                    case "channelupdated":
                        profile.LogConfig.ChannelUpdated = !profile.LogConfig.ChannelUpdated;
                        break;
                    case "channeldeleted":
                        profile.LogConfig.ChannelDeleted = !profile.LogConfig.ChannelDeleted;
                        break;
                    case "channelcreated":
                        profile.LogConfig.ChannelCreated = !profile.LogConfig.ChannelCreated;
                        break;
                }
            }

            await ctx.RespondAsync( $"Toggled the following log events: {string.Join(", ",eventTypes)}" );

            if (!profile.LogConfig.LoggingEnabled)
            {
                await ctx.RespondAsync( " Logging is disabled for this server, did you forget to enable it?" );
            }

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
        }
    }
}
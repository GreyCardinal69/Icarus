﻿using System;
using System.Linq;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;

using Icarus.Modules.Profiles;
using Newtonsoft.Json;
using System.IO;

namespace Icarus.Modules.Logs
{
    public class LogManagement : BaseCommandModule
    {
        [Command( "enableLogging" )]
        [Description( "Enables logging for the server executed in, logs go into the specified channel." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.Administrator )]
        public async Task EnableLogging ( CommandContext ctx, ulong channelId )
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

            ServerProfile Profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            Program.Core.ServerProfiles.First( x => x.ID == ctx.Guild.Id ).LogConfig.LogChannel = channelId;
            Profile.LogConfig.ToggleLogging( true );
            await ctx.RespondAsync( $"Enabled logging for {ctx.Guild.Name} in: {ctx.Guild.GetChannel( channelId )}" );

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( Profile, Formatting.Indented ) );
        }

        [Command( "disableLogging" )]
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

            ServerProfile Profile = ServerProfile.ProfileFromId( ctx.Guild.Id );
            Profile.LogConfig.LogChannel = channelId;

            Program.Core.ServerProfiles.Add( Profile );
            Profile.LogConfig.ToggleLogging( false );
            await ctx.RespondAsync( $"Disabled logging for {ctx.Guild.Name} in: {ctx.Guild.GetChannel( channelId )}" );

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( Profile, Formatting.Indented ) );
        }

        [Command( "toggleLogEvents" )]
        [Description( "Toggles log events for the server executed in, invalid events will be ignored." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.Administrator )]
        public async Task EnableLogEvents ( CommandContext ctx, params string[] EventTypes )
        {
            await ctx.TriggerTypingAsync();

            if (EventTypes.Length < 1)
            {
                await ctx.RespondAsync( "Specify at least one log event to toggle on/off." );
                return;
            }

            if (!Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ))
            {
                await ctx.RespondAsync( "Server is not registered, can not toggle log events." );
                return;
            }

            ServerProfile Profile = ServerProfile.ProfileFromId( ctx.Guild.Id );

            foreach (var Event in EventTypes)
            {
                switch (Event)
                {
                    case "guildmemberremoved":
                        Profile.LogConfig.GuildMemberRemoved = !Profile.LogConfig.GuildMemberRemoved;
                        break;
                    case "guildmemberadded":
                        Profile.LogConfig.GuildMemberAdded = !Profile.LogConfig.GuildMemberAdded;
                        break;
                    case "guildbanremoved":
                        Profile.LogConfig.GuildBanRemoved = !Profile.LogConfig.GuildBanRemoved;
                        break;
                    case "guildbanadded":
                        Profile.LogConfig.GuildBanAdded = !Profile.LogConfig.GuildBanAdded;
                        break;
                    case "guildrolecreated":
                        Profile.LogConfig.GuildRoleCreated = !Profile.LogConfig.GuildRoleCreated;
                        break;
                    case "guildroleupdated":
                        Profile.LogConfig.GuildRoleUpdated = !Profile.LogConfig.GuildRoleUpdated;
                        break;
                    case "guildroledeleted":
                        Profile.LogConfig.GuildRoleDeleted = !Profile.LogConfig.GuildRoleDeleted;
                        break;
                    case "messagereactionscleared":
                        Profile.LogConfig.MessageReactionsCleared = !Profile.LogConfig.MessageReactionsCleared;
                        break;
                    case "messagereactionremoved":
                        Profile.LogConfig.MessageReactionRemoved = !Profile.LogConfig.MessageReactionRemoved;
                        break;
                    case "messagereactionadded":
                        Profile.LogConfig.MessageReactionAdded = !Profile.LogConfig.MessageReactionAdded;
                        break;
                    case "messagesbulkdeleted":
                        Profile.LogConfig.MessagesBulkDeleted = !Profile.LogConfig.MessagesBulkDeleted;
                        break;
                    case "messagedeleted":
                        Profile.LogConfig.MessageDeleted = !Profile.LogConfig.MessageDeleted;
                        break;
                    case "messageupdated":
                        Profile.LogConfig.MessageUpdated = !Profile.LogConfig.MessageUpdated;
                        break;
                    case "messagecreated":
                        Profile.LogConfig.MessageCreated = !Profile.LogConfig.MessageCreated;
                        break;
                    case "invitedeleted":
                        Profile.LogConfig.InviteDeleted = !Profile.LogConfig.InviteDeleted;
                        break;
                    case "invitecreated":
                        Profile.LogConfig.InviteCreated = !Profile.LogConfig.InviteCreated;
                        break;
                    case "channelupdated":
                        Profile.LogConfig.ChannelUpdated = !Profile.LogConfig.ChannelUpdated;
                        break;
                    case "channeldeleted":
                        Profile.LogConfig.ChannelDeleted = !Profile.LogConfig.ChannelDeleted;
                        break;
                    case "channelcreated":
                        Profile.LogConfig.ChannelCreated = !Profile.LogConfig.ChannelCreated;
                        break;
                }
            }

            await ctx.RespondAsync( $"Toggled the following log events: {string.Join( ", ", LogProfile.LogEvents.Where( X => EventTypes.Contains( X.ToLower() ) ) )}" );

            Program.Core.ServerProfiles.First( x => x.ID == Profile.ID ).LogConfig = Profile.LogConfig;

            if (!Profile.LogConfig.LoggingEnabled)
            {
                await ctx.RespondAsync(" Logging is disabled for this server, did you forget to enable it?");
            }

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.json", JsonConvert.SerializeObject( Profile, Formatting.Indented ) );
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using GreyCrammedContainer;

using Icarus.Modules.Profiles;

namespace Icarus.Modules.Logs
{
    public class LogManagement : BaseCommandModule
    {
        [Command( "EnableLogging" )]
        [Description( "Enables logging for the server executed in, logs go into the specified channel." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.Administrator )]
        public async Task EnableLogging ( CommandContext ctx, ulong ChannelId)
        {
            await ctx.TriggerTypingAsync();

            if (!Program.Core.RegisteredServerIds.Contains(ctx.Guild.Id))
            {
                await ctx.RespondAsync("Server is not registered, can not enable logging.");
                return;
            }

            if (!ctx.Guild.Channels.ContainsKey(ChannelId))
            {
                await ctx.RespondAsync($"Invalid channel Id: {ChannelId}");
                return;
            }

            ServerProfile Profile = ServerProfile.ProfileFromId( ctx.Guild.Id );
            Profile.LogConfig.LogChannel = ChannelId;

            if (Profile.LogConfig.LoggingEnabled)
            {
                Profile.LogConfig.ToggleLogging( false );
                await ctx.RespondAsync( $"Disabled logging for {ctx.Guild.Name} in: {ctx.Guild.GetChannel( ChannelId )}" );
                Program.Core.EnabledLogs.Remove( Profile ) ;
            }
            else
            {
                Program.Core.EnabledLogs.Add( Profile );
                Profile.LogConfig.ToggleLogging( true );
                await ctx.RespondAsync( $"Enabled logging for {ctx.Guild.Name} in: {ctx.Guild.GetChannel(ChannelId)}" );
            }

            GccConverter.Serialize( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.gcc", Profile );
        }

        [Command( "EnableLogEvents" )]
        [Description( "Enables log events for the server executed in, invalid events will be ignored." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.Administrator )]
        public async Task EnableLogEvents ( CommandContext ctx, params string[] EventTypes )
        {
            await ctx.TriggerTypingAsync();

            if (EventTypes.Length < 1)
            {
                await ctx.RespondAsync("Specify at least one log event to enable.");
                return;
            }

            if (!Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ))
            {
                await ctx.RespondAsync( "Server is not registered, can not enable log events." );
                return;
            }

            ServerProfile Profile = ServerProfile.ProfileFromId(ctx.Guild.Id);
            List<string> Events = LogConfig.LogEvents().Select(X => X.ToLower()).ToList();
            
            foreach (var Event in EventTypes)
            {
                if (Events.Contains(Event))
                {
                    switch (Event)
                    {
                        case "guildmemberremoved":
                            Profile.LogConfig.GuildMemberRemoved = true;
                            break;
                        case "guildmemberadded":
                            Profile.LogConfig.GuildMemberAdded = true;
                            break;
                        case "guildbanremoved":
                            Profile.LogConfig.GuildBanRemoved = true;
                            break;
                        case "guildbanadded":
                            Profile.LogConfig.GuildBanAdded = true;
                            break;
                        case "guildrolecreated":
                            Profile.LogConfig.GuildRoleCreated = true;
                            break;
                        case "guildroleupdated":
                            Profile.LogConfig.GuildRoleUpdated = true;
                            break;
                        case "guildroledeleted":
                            Profile.LogConfig.GuildRoleDeleted = true;
                            break;
                        case "messagereactionscleared":
                            Profile.LogConfig.MessageReactionsCleared = true;
                            break;
                        case "messagereactionremoved":
                            Profile.LogConfig.MessageReactionRemoved = true;
                            break;
                        case "messagereactionadded":
                            Profile.LogConfig.MessageReactionAdded = true;
                            break;
                        case "messagesbulkdeleted":
                            Profile.LogConfig.MessagesBulkDeleted = true;
                            break;
                        case "messagedeleted":
                            Profile.LogConfig.MessageDeleted = true;
                            break;
                        case "messageupdated":
                            Profile.LogConfig.MessageUpdated = true;
                            break;
                        case "messagecreated":
                            Profile.LogConfig.MessageCreated = true;
                            break;
                        case "invitedeleted":
                            Profile.LogConfig.InviteDeleted = true;
                            break;
                        case "invitecreated":
                            Profile.LogConfig.InviteCreated = true;
                            break;
                        case "channelupdated":
                            Profile.LogConfig.ChannelUpdated = true;
                            break;
                        case "channeldeleted":
                            Profile.LogConfig.ChannelDeleted = true;
                            break;
                        case "channelcreated":
                            Profile.LogConfig.ChannelCreated = true;
                            break;
                    }
                }
            }

            await ctx.RespondAsync($"Activated the following log events: {string.Join( ", ", Events.Where(X => EventTypes.Contains(X) ))}");
            GccConverter.Serialize( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.gcc", Profile );
        }

        [Command( "DisableLogEvents" )]
        [Description( "Disables log events for the server executed in, invalid events will be ignored." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.Administrator )]
        public async Task DisableLogEvents ( CommandContext ctx, params string[] EventTypes )
        {
            await ctx.TriggerTypingAsync();

            if (EventTypes.Length < 1)
            {
                await ctx.RespondAsync( "Specify at least one log event to disable." );
                return;
            }

            if (!Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ))
            {
                await ctx.RespondAsync( "Server is not registered." );
                return;
            }

            ServerProfile Profile = ServerProfile.ProfileFromId( ctx.Guild.Id );
            List<string> Events = LogConfig.LogEvents().Select( X => X.ToLower() ).ToList();

            foreach (var Event in EventTypes)
            {
                if (Events.Contains( Event ))
                {
                    switch (Event)
                    {
                        case "guildmemberremoved":
                            Profile.LogConfig.GuildMemberRemoved = false;
                            break;
                        case "guildmemberadded":
                            Profile.LogConfig.GuildMemberAdded = false;
                            break;
                        case "guildbanremoved":
                            Profile.LogConfig.GuildBanRemoved = false;
                            break;
                        case "guildbanadded":
                            Profile.LogConfig.GuildBanAdded = false;
                            break;
                        case "guildrolecreated":
                            Profile.LogConfig.GuildRoleCreated = false;
                            break;
                        case "guildroleupdated":
                            Profile.LogConfig.GuildRoleUpdated = false;
                            break;
                        case "guildroledeleted":
                            Profile.LogConfig.GuildRoleDeleted = false;
                            break;
                        case "messagereactionscleared":
                            Profile.LogConfig.MessageReactionsCleared = false;
                            break;
                        case "messagereactionremoved":
                            Profile.LogConfig.MessageReactionRemoved = false;
                            break;
                        case "messagereactionadded":
                            Profile.LogConfig.MessageReactionAdded = false;
                            break;
                        case "messagesbulkdeleted":
                            Profile.LogConfig.MessagesBulkDeleted = false;
                            break;
                        case "messagedeleted":
                            Profile.LogConfig.MessageDeleted = false;
                            break;
                        case "messageupdated":
                            Profile.LogConfig.MessageUpdated = false;
                            break;
                        case "messagecreated":
                            Profile.LogConfig.MessageCreated = false;
                            break;
                        case "invitedeleted":
                            Profile.LogConfig.InviteDeleted = false;
                            break;
                        case "invitecreated":
                            Profile.LogConfig.InviteCreated = false;
                            break;
                        case "channelupdated":
                            Profile.LogConfig.ChannelUpdated = false;
                            break;
                        case "channeldeleted":
                            Profile.LogConfig.ChannelDeleted = false;
                            break;
                        case "channelcreated":
                            Profile.LogConfig.ChannelCreated = false;
                            break;
                    }
                }
            }

            await ctx.RespondAsync( $"Disabled the following log events: {string.Join( ", ", Events.Where( X => EventTypes.Contains( X ) ) )}" );
            GccConverter.Serialize( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}.gcc", Profile );
        }
    }
}
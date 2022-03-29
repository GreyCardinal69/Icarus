using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Icarus.Modules.Profiles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Modules.Other
{
    public class GeneralCommands : BaseCommandModule
    {
        [Command( "ping" )]
        [Description( "Responds with ping time." )]
        public async Task Ping ( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync( $"Ping: {ctx.Client.Ping}ms" );
        }

        [Command( "erase" )]
        [Description( "Deletes set amount of messages if possible." )]
        [RequireUserPermissions( DSharpPlus.Permissions.ManageMessages )]
        public async Task Erase ( CommandContext ctx, int count )
        {
            await ctx.TriggerTypingAsync();
            try
            {
                var messages = await ctx.Channel.GetMessagesAsync( count );
                await ctx.Channel.DeleteMessagesAsync( messages );
                await ctx.RespondAsync( $"Erased: {count} messages, called by {ctx.User.Mention}." );
            }
            catch (Exception)
            {
                await ctx.RespondAsync( "Failed to erase, are the messages too old?" );
            }
        }

        [Command( "eraseFromTo" )]
        [Description( "Deletes all messages from the first to the second specified message." )]
        [RequireUserPermissions( DSharpPlus.Permissions.ManageMessages )]
        public async Task EraseFromTo ( CommandContext ctx, ulong from, ulong to, int amount )
        {
            await ctx.TriggerTypingAsync();
            var fromMsg = await ctx.Channel.GetMessageAsync( from );
            var toMsg = await ctx.Channel.GetMessageAsync( to );

            var messagesBefore = await ctx.Channel.GetMessagesBeforeAsync( to, amount );
            var messagesAfter = await ctx.Channel.GetMessagesAfterAsync( from, amount );

            var filtered = messagesAfter.Union( messagesBefore ).Distinct().Where(
                x => ( DateTimeOffset.UtcNow - x.Timestamp ).TotalDays <= 14 &&
                x.Timestamp <= toMsg.Timestamp && x.Timestamp >= fromMsg.Timestamp
            );

            await ctx.Channel.DeleteMessagesAsync( filtered );
            await ctx.RespondAsync( $"Erased: {filtered.Count()} messages, called by {ctx.User.Mention}." );
        }

        [Command( "ban" )]
        [Description( "Bans a user with optional amount of messages to delete." )]
        [RequireUserPermissions( DSharpPlus.Permissions.BanMembers )]
        public async Task Ban ( CommandContext ctx, ulong id, int deleteAmount = 0, string reason = "" )
        {
            await ctx.TriggerTypingAsync();

            var user = JsonConvert.DeserializeObject<UserProfile>(
                  File.ReadAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{id}.json" ) );

            user.LeaveDate = DateTime.UtcNow;
            user.BanEntries.Add( new Tuple<DateTime, string>(DateTime.UtcNow, reason) );

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{id}.json",
                 JsonConvert.SerializeObject( user, Formatting.Indented ) );

            await ctx.Guild.BanMemberAsync( id, deleteAmount, reason );
            await ctx.RespondAsync( $"Banned {ctx.Guild.GetMemberAsync( id ).Result.Mention}, deleted last {deleteAmount} messages with \"{reason}\" as reason." );
        }

        [Command( "kick" )]
        [Description( "Kicks a user with an optional reason." )]
        [RequireUserPermissions( DSharpPlus.Permissions.KickMembers )]
        public async Task Kick ( CommandContext ctx, ulong id, string reason = "" )
        {
            await ctx.TriggerTypingAsync();

            var user = JsonConvert.DeserializeObject<UserProfile>(
                  File.ReadAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{id}.json" ) );

            user.LeaveDate = DateTime.UtcNow;
            user.KickEntries.Add( new Tuple<DateTime, string>( DateTime.UtcNow, reason ) );

            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{id}.json",
                 JsonConvert.SerializeObject( user, Formatting.Indented ) );

            await ctx.Guild.GetMemberAsync( id ).Result.RemoveAsync();
            await ctx.RespondAsync( $"Kicked {ctx.Guild.GetMemberAsync( id ).Result.Mention}, with \"{reason}\" as reason." );
        }

        [Command( "unban" )]
        [Description( "Unbans a user." )]
        [RequireUserPermissions( DSharpPlus.Permissions.BanMembers )]
        public async Task Unban ( CommandContext ctx, ulong id )
        {
            await ctx.TriggerTypingAsync();
            await ctx.Guild.UnbanMemberAsync( id );
            await ctx.RespondAsync( $"Unbanned {ctx.Guild.GetMemberAsync( id ).Result.Mention}." );
        }

        [Command( "reportServers" )]
        [Description( "Responds with information on serving servers." )]
        [RequireOwner]
        public async Task ReportServers ( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync( $"Watching { string.Join( ",\n  \t\t\t\t ", ctx.Client.Guilds.Values.ToList() ) }" );
        }

        [Command( "setStatus" )]
        [Description( "Sets the bot's status." )]
        [RequireOwner]
        public async Task SetActivity ( CommandContext ctx, int type, [RemainingText] string status )
        {
            if (ctx.User.Id == Program.Core.OwnerId)
            {
                DiscordActivity activity = new();
                DiscordClient discord = ctx.Client;
                activity.Name = status;
                // Offline = 0,
                // Online = 1,
                // Idle = 2,
                // DoNotDisturb = 4,
                // Invisible = 5
                await discord.UpdateStatusAsync( activity, (UserStatus)type, DateTimeOffset.UtcNow );
                return;
            }
            else
            {
                await ctx.RespondAsync("No way this part of code will ever be executed.");
            }
        }
    }
}
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
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

        [Command( "ban" )]
        [Description( "Bans a user with optional amount of messages to delete." )]
        [RequireUserPermissions( DSharpPlus.Permissions.BanMembers )]
        public async Task Ban ( CommandContext ctx, ulong id, int deleteAmount = 0, string reason = "" )
        {
            await ctx.TriggerTypingAsync();
            await ctx.Guild.BanMemberAsync( id, deleteAmount, reason );
            await ctx.RespondAsync( $"Banned {ctx.Guild.GetMemberAsync( id ).Result.Mention}, deleted last {deleteAmount} messages with \"{reason}\" as reason." );
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
    }
}
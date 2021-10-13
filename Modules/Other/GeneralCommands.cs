using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
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

        [Command( "ReportServers" )]
        [Description( "Responds with information on serving servers." )]
        [RequireOwner]
        public async Task ReportServers ( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync( $"Watching { string.Join(",\n  \t\t\t\t ", ctx.Client.Guilds.Values.ToList()) }" );
        }

        [Command( "erase" )]
        [Description( "Deletes set amount of messages if possible." )]
        [RequireUserPermissions(DSharpPlus.Permissions.ManageMessages)]
        public async Task Erase ( CommandContext ctx, int Count )
        {
            await ctx.TriggerTypingAsync();
            try
            {
                var messages = await ctx.Channel.GetMessagesAsync( Count );
                await ctx.Channel.DeleteMessagesAsync( messages );
                await ctx.RespondAsync( $"Erased: " + Count + " messages, instantiated by " + ctx.User.Username );
            }
            catch (Exception)
            {
                await ctx.RespondAsync( "Failed to erase, are the messages too old?" );
            }
        }

        [Command( "ban" )]
        [Description( "Bans a user with optional amount of messages to delete." )]
        [RequireUserPermissions( DSharpPlus.Permissions.BanMembers )]
        public async Task Ban ( CommandContext ctx, ulong ID, int DeleteAmount = 0, string Reason = "" )
        {
            await ctx.TriggerTypingAsync();
            await ctx.Guild.BanMemberAsync(ID, DeleteAmount, Reason );
            await ctx.RespondAsync($"Banned {ctx.Guild.GetMemberAsync(ID).Result.Mention}, deleted last {DeleteAmount} messages with \"{Reason}\" as reason.");
        }

        [Command( "unban" )]
        [Description( "Unbans a user." )]
        [RequireUserPermissions( DSharpPlus.Permissions.BanMembers )]
        public async Task UnBan ( CommandContext ctx, ulong ID)
        {
            await ctx.TriggerTypingAsync();
            await ctx.Guild.UnbanMemberAsync( ID );
            await ctx.RespondAsync( $"Unbanned {ctx.Guild.GetMemberAsync( ID ).Result.Mention}" );
        }









    }
}
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.SlashCommands;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System;

namespace Icarus.Modules.Other
{
    public class SlashCommands : ApplicationCommandModule
    {
        [SlashCommand( "test", "A slash command made to test the DSharpPlus Slash Commands extension!" )]
        public async Task TestCommand( InteractionContext ctx )
        {
            await ctx.CreateResponseAsync( InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent( "Success!" ) );
        }
    }
}
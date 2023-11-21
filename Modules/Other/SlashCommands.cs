using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.SlashCommands;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus;

namespace Icarus.Modules.Other
{
    public class SlashCommands : ApplicationCommandModule
    {
        [SlashCommand( "test", "A slash command made to test the DSharpPlus Slash Commands extension!" )]
        public async Task TestCommand( InteractionContext ctx )
        {
            await ctx.CreateResponseAsync( InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent( "Your home address, coordinates and credit card information has been added to the GreySoc database, we thank you for your obedience." ) );
        }
    }
}
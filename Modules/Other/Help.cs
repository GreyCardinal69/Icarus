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
    public class Help : BaseCommandModule
    {
        [Command( "help" )]
        [Description( "Responds with information on available command categories." )]
        public async Task HelpBasic ( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            var embed = new DiscordEmbedBuilder
            {
                Title = "Commands:",
                Color = DiscordColor.SpringGreen,
                Description =
                $"Listing command categories. \n Type `>help <command>` to get more info on the specified command. \n\n **Categories**\n" +
                $"General\nDeveloper\nLogs\nModerator\nMiscellanea",
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    IconUrl = ctx.Member.AvatarUrl,
                },
                Timestamp = DateTime.Now,
            };
            await ctx.RespondAsync( embed );
        }

        [Command( "help" )]
        [Description( "Responds with information on given command category commands." )]
        public async Task HelpCategory ( CommandContext ctx, string category )
        {
            switch (category.ToLower())
            {
                default:
                    //await ctx.RespondAsync("sad");
                    break;
            }
        }
    }
}
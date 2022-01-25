using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Modules.Other
{
    public class Help : BaseCommandModule
    {
        [Command( "Helpr" )]
        [Description( "Responds with information on available commands." )]
        public async Task Helpr ( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync( 
                $"helpr\nping\nIsolate\n"
            );
        }



    }
}
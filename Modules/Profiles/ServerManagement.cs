using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GreyCrammedContainer;
using DSharpPlus.CommandsNext;
using System.IO;

namespace Icarus.Modules.Profiles
{
    public class ServerManagement : BaseCommandModule
    {
        [Command( "RegisterServer" )]
        [Description( "Creates a server profile for the server where executed." )]
        [Require​User​Permissions​Attribute(DSharpPlus.Permissions.Administrator)]
        public async Task RegisterServer ( CommandContext ctx, bool OverWrite = false )
        {
            await ctx.TriggerTypingAsync();
            string ProfilesPath = AppDomain.CurrentDomain.BaseDirectory + @$"\ServerProfiles\";

            if (File.Exists( $"{ProfilesPath}{ctx.Guild.Id}.gcc") && !OverWrite)
            {
                await ctx.RespondAsync($"A server profile for this server already exists, do you want to reset it ? If yes type `%registerserver true`");
                return;
            }
            
            ServerProfile Profile = new()
            {
                Name = ctx.Guild.Name,
                ID = ctx.Guild.Id
            };
            GccConverter.Serialize($"{ProfilesPath}{ctx.Guild.Id}.gcc", Profile);
            await ctx.RespondAsync($"Created a new server profile for {ctx.Guild.Name}.");
        }














    }
}
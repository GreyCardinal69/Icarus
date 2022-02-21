using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;

using Icarus.Modules.Profiles;
using Newtonsoft.Json;
using System.IO;
using DSharpPlus.Interactivity.Extensions;

namespace Icarus.Modules.Servers
{
    public class ServerManagement : BaseCommandModule
    {
        [Command( "registerServer" )]
        [Description( "Creates a server profile for the server where executed." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.Administrator )]
        public async Task RegisterServer ( CommandContext ctx, bool OverWrite = false )
        {
            await ctx.TriggerTypingAsync();
            string ProfilesPath = AppDomain.CurrentDomain.BaseDirectory + @$"\ServerProfiles\";

            if (File.Exists( $"{ProfilesPath}{ctx.Guild.Id}.json" ) && !OverWrite)
            {
                await ctx.RespondAsync( $"A server profile for this server already exists, do you want to overwrite it ? If yes type `>registerserver true`" );
                return;
            }

            ServerProfile Profile = new()
            {
                Name = ctx.Guild.Name,
                ID = ctx.Guild.Id,
                ProfileCreationDate = DateTime.UtcNow
            };

            File.WriteAllText( $"{ProfilesPath}{ctx.Guild.Id}.json", JsonConvert.SerializeObject( Profile, Formatting.Indented ) );
            await ctx.RespondAsync( $"Created a new server profile for {ctx.Guild.Name}." );
        }

        [Command( "deleteServer" )]
        [Description( "Creates a server profile for the server where executed." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.Administrator )]
        public async Task RegisterServer ( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            string ProfilesPath = AppDomain.CurrentDomain.BaseDirectory + @$"\ServerProfiles\";

            await ctx.RespondAsync( " Confirm action by responding with \"yes\" " );

            var interactivity = ctx.Client.GetInteractivity();
            var msg = await interactivity.WaitForMessageAsync
            (
                xm => string.Equals(xm.Content, "yes",
                StringComparison.InvariantCultureIgnoreCase),
                TimeSpan.FromSeconds( 60 )
            );

            if (!msg.TimedOut)
            {
                File.Delete( $"{ProfilesPath}{ctx.Guild.Id}.json" );
                await ctx.RespondAsync( $"Deleted the server profile for {ctx.Guild.Name}." );
            }
            else
            {
                await ctx.RespondAsync( "Confirmation time ran out, aborting." );
            }
        }
    }
}
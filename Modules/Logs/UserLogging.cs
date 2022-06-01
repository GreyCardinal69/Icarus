using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;

using Icarus.Modules.Profiles;

namespace Icarus.Modules.Logs
{
    public class UserLogging : BaseCommandModule
    {
        [Command( "registerUsers" )]
        [Description( "Creates profiles for all server users." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.Administrator )]
        public async Task RegisterUsers ( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();

            var users = ctx.Guild.GetAllMembersAsync().Result;

            foreach (var user in users)
            {
                var profile = new UserProfile( user.Id, user.Username )
                {
                    Discriminator = user.Discriminator,
                    CreationDate = user.CreationTimestamp,
                    FirstJoinDate = user.JoinedAt,
                    LocalLanguage = user.Locale
                };
                Directory.CreateDirectory( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\" );
                File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{user.Id}.json" ,
                    JsonConvert.SerializeObject( profile, Formatting.Indented ) );
            }

            await ctx.RespondAsync( $"Created {users.Count} user profiles." );
        }

        [Command( "userProfile" )]
        [Description( "Responds with information on a user's profile." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageRoles )]
        public async Task Profile ( CommandContext ctx, ulong id )
        {
            await ctx.TriggerTypingAsync();

            if (ctx.Guild.GetMemberAsync( id ).Result == null)
            {
                await ctx.RespondAsync( "Invalid User Id." );
                return;
            }

            var user = JsonConvert.DeserializeObject<UserProfile>(
                File.ReadAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{id}.json" ) );

            List<string> banEntries = new();
            foreach (var item in user.BanEntries)
            {
                banEntries.Add( $"{item.Item1}  :  {item.Item2}" );
            }

            List<string> kickEntries = new();
            foreach (var item in user.KickEntries)
            {
                kickEntries.Add( $"{item.Item1}  :  {item.Item2}" );
            }

            List<string> punishmentEntries = new();
            foreach (var item in user.PunishmentEntries)
            {
                punishmentEntries.Add( $"{item.Item1}  :  {item.Item2}" );
            }

            var bans = banEntries.Count == 0 ? "None" : string.Join( "\n", banEntries );
            var kicks = kickEntries.Count == 0 ? "None" : string.Join( "\n", kickEntries );
            var strikes = punishmentEntries.Count == 0 ? "None" : $"\n{string.Join( "\n", punishmentEntries )}";

            var embed = new DiscordEmbedBuilder
            {
                Title = $"Profile {ctx.Guild.GetMemberAsync(id).Result.Username}",
                Color = DiscordColor.SpringGreen,
                Description =
                    $"The user's id is: {user.ID}.\n Discriminator: #{user.Discriminator}.\n" +
                    $"The account was created at {user.CreationDate}.\n The user first joined at: {user.FirstJoinDate}.\n" +
                    $"The user last left the server at {user.LeaveDate}.\n\n The user's logged ban entries are: {bans}.\n\n" +
                    $"The user's logged kick entries are: {kicks}.\n\n" +
                    $"The user's logged punishment entries are: {strikes}.\n\n",
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    IconUrl = ctx.Client.CurrentUser.AvatarUrl,
                },
                Timestamp = DateTime.Now
            };

            await ctx.RespondAsync( embed );
        }
    }
}
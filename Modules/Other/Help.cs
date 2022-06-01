using System;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Icarus.Modules.Other
{
    public class Help : BaseCommandModule
    {
        [Command( "Help" )]
        [Description( "Responds with information on available command categories." )]
        public async Task HelpBasic ( CommandContext ctx, string category = "")
        {
            await ctx.TriggerTypingAsync();
            DiscordEmbedBuilder embed;

            if (category == "")
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "Commands:",
                    Color = DiscordColor.SpringGreen,
                    Description =
                    $"Listing command categories. \n Type `>help <command>` to get more info on the specified command. \n\n **Categories**\n" +
                    $"Isolation\nLogging\nGeneral\nServer",
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        IconUrl = ctx.Member.AvatarUrl,
                    },
                    Timestamp = DateTime.Now,
                };
                await ctx.RespondAsync( embed );
                return;
            }

            switch (category.ToLower())
            {
                case "isolation":
                    embed = new DiscordEmbedBuilder
                    {
                        Title = "Isolation Commands",
                        Color = DiscordColor.SpringGreen,
                        Description =
                        $"`isolate <punishmentRoleId> <userId> <channelId> <time> <returnRolesOnRelease> <reason>`: " +
                        $"Isolates a user at a channel for a given time, with an option to give back revoked " +
                        $"roles on release and a reason. The time can be either (x)d or (x)m, d for " +
                        $"days, m for months. For example 5d or 1.5m.\n\n" +
                        $"`releaseUser: <userId>`: Releases a user from isolation.",
                        Author = new DiscordEmbedBuilder.EmbedAuthor
                        {
                            IconUrl = ctx.Member.AvatarUrl,
                        },
                        Timestamp = DateTime.Now,
                    };
                    await ctx.RespondAsync( embed );
                    break;
                case "logging":
                    embed = new DiscordEmbedBuilder
                    {
                        Title = "Log Commands",
                        Color = DiscordColor.SpringGreen,
                        Description =
                            $"`enableLogging <channelId>`: Enables logging for the server executed in, " +
                            $"non important notifications go into the specified channel.\n\n" +
                            $"`setMajorLogChannel <channelId>`: Sets the channel for major notifications.\n\n" +
                            $"`addWordsBL <array of words>`: Adds the specified words into the server's " +
                            $"black-listed word list. Any mentions of those will be reported to the major notifications channel.\n\n" +
                            $"`removeWordsBL <array of words>`: Removes the specified words from the server's black-listed words.\n\n" +
                            $"`setContainmentDefaults <channelId> <roleId>`: Sets default containment channel and " +
                            $"containment role ids.\n\n" +
                            $"`disableLogging <channelId>`: Disables logging (minor) for the server executed in, logs go into the specified channel.\n\n" +
                            $"`toggleLogEvents <array of events>`: Toggles log events for the server executed in, invalid events will be ignored.\n\n" +
                            $"`logEvents`: Lists available log events.\n\n" +
                            $"`registerUsers`: Creates profiles for all server users. Must be called when setting up " +
                            $"the bot for the server.\n\n" +
                            $"`userProfile <userId>`: Responds with information on a registered user's profile.\n\n",
                        Author = new DiscordEmbedBuilder.EmbedAuthor
                        {
                            IconUrl = ctx.Member.AvatarUrl,
                        },
                        Timestamp = DateTime.Now,
                    };
                    await ctx.RespondAsync( embed );
                    break;
                case "general":
                    embed = new DiscordEmbedBuilder
                    {
                        Title = "General Commands",
                        Color = DiscordColor.SpringGreen,
                        Description =
                            $"`ping`: Responds with client ping time.\n\n" +
                            $"`erase <count>`: Deletes set amount of messages if possible.\n\n" +
                            $"`eraseFromTo <from> <to> <amount>`: Deletes set amount of messages from the first " +
                            $"to the second specified message.\n\n" +
                            $"`ban <userId> <messageAmount> <reason>`: Bans a user with optional amount of messages to delete, " +
                            $"with a reason for the undertaken action.\n\n" +
                            $"`kick <userId> <reason>`: Kicks a user with a specified reason.\n\n" +
                            $"`unban <userId>`: Unbans the user.\n\n" +
                            $"`reportServers (DEV)`: Responds with information on serving servers.\n\n" +
                            $"`setStatus <type> <status> (DEV)`: Sets the bot's status.\n\n",
                        Author = new DiscordEmbedBuilder.EmbedAuthor
                        {
                            IconUrl = ctx.Member.AvatarUrl,
                        },
                        Timestamp = DateTime.Now,
                    };
                    await ctx.RespondAsync( embed );
                    break;
                case "server":
                    embed = new DiscordEmbedBuilder
                    {
                        Title = "Server Commands",
                        Color = DiscordColor.SpringGreen,
                        Description =
                            $"`registerProfile <overWrite>`: Creates a server profile for the server where executed. " +
                            $"if <overWrite> is true replaces the existing profile for the server with a new one.\n\n" +
                            $"`confAntiSpam <first> <second> <third> <limit>`: Changes server anti spam module configurations. " +
                            $"First warning is given when within the interval ( 20 seconds ) a user sends <first> amount of messages, " +
                            $"the second when <second> amount of messages are sent, last if <third> amount of messages, " +
                            $"on reaching <limit> the user's actions are considered spam and he is isolated at the " +
                            $"default containment channel. A notification is sent to the major notifications channel.\n\n" +
                            $"`antiSpamIgnore <array of channel ids>`: Tells the anti spam module to ignore the specified channels.\n\n" +
                            $"`antiSpamReset`: Tells the anti spam to no longer ignore any channels in the server.\n\n" +
                            $"`deleteProfile`: Deletes the server profile of the server.\n\n" +
                            $"`profile`: Responds with information on the server profile.\n\n",
                        Author = new DiscordEmbedBuilder.EmbedAuthor
                        {
                            IconUrl = ctx.Member.AvatarUrl,
                        },
                        Timestamp = DateTime.Now,
                    };
                    await ctx.RespondAsync( embed );
                    break;
                default:
                    await ctx.RespondAsync( "No category found." );
                    break;
            }
        }
    }
}
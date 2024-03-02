using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace Icarus.Modules.Other
{
    public class Help : BaseCommandModule
    {
        [Command( "Help" )]
        [Description( "Responds with information on available command categories." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageMessages )]
        public async Task HelpBasic ( CommandContext ctx, params string[] text )
        {
            await ctx.TriggerTypingAsync();
            DiscordEmbedBuilder embed;

            var category = string.Join( " ", text );

            if (category == "")
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "Commands:",
                    Color = DiscordColor.SpringGreen,
                    Description =
                    $"Listing command categories. \n Type `>help <category>` to get more info on the specified category. \n\n **Categories**\n" +
                    $"Isolation\nLogging\nGeneral\nServer\nServer Specific",
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
                            $"`eraseAggressive <count>`: Deletes set amount of messages if possible. Can delete messages older than 2 weeks.\n\n" +
                            $"`eraseFromTo <from> <to> <amount>`: Deletes set amount of messages from the first " +
                            $"to the second specified message.\n\n" +
                            $"`archive <channelId>`: Exports a discord channel and sends it as a .zip file\n\n" +
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
                case "server specific":
                    embed = new DiscordEmbedBuilder
                    {
                        Title = "Server Specific Commands",
                        Color = DiscordColor.SpringGreen,
                        Description =
                           $"( For Event Horizon Official )\n" +
                           $"`report <date> <channelId> <state> <thresholds> <optional_format> <comment(s)>`: Creates an activity report for the channel. Starting " +
                           $"from the first message after `<date>` in the channel with `<channelId>` ( mention the channel ). The `<state>` describes the state of the channel in the time period. The `<thresholds>` are " +
                           $"responsible for categorizing user activity, `<optional_format>` is used for user list table format and `<comment(s)>` are additional comments to describe events or to take notes. " +
                           $"Words in the `<state>` must by connected by '-'. Thresholds are connected by '/' and follow this format `name-min:max`. For a new line in comments add `\\n`. Available table formats are `default` and `md`. If you dont specify a format, it will use the `default` ( dont write `default` ). The `<date>` is given as such `day/month/year` aka `int/first three letters of the month/year`.\n" +
                           $"Example usage: `>report 28-Oct-23 #ehce Doing-Fine Dead-0:9/Inactive-10:25/SemiActive-26:35/Active-36:70/VeryActive-70:1000 No comments.`\n Or \n" +
                           $"`>report 28-Oct-23 #ehce Doing-Fine Dead-0:9/Inactive-10:25/SemiActive-26:35/Active-36:70/VeryActive-70:1000 md No comments.`" ,
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
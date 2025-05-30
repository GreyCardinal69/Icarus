﻿using DSharpPlus.CommandsNext;
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
        public async Task HelpCommand ( CommandContext ctx, params string[] text )
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
                            $"`AddLogExclusion <channelId>`: Disables logging in a specific channel.\n\n" +
                            $"`RemoveLogExclusion <channelId>`: Enables logging in a specific channel after it has been excluded from logging.\n\n" +
                            $"`ListLogExclusions`: Replies with channels not logged.\n\n" +
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
                            $"if `<overWrite>` is true replaces the existing profile for the server with a new one.\n\n" +
                            $"`EnableAntiSpam <first> <second> <third> <limit>`: Enabled and sets the server anti spam module configurations. " +
                            $"First warning is given when within the interval ( 20 seconds ) a user sends `<first>` amount of messages, " +
                            $"the second when `<second>` amount of messages are sent, last if `<third>` amount of messages, " +
                            $"on reaching `<limit>` the user's actions are considered spam and he is isolated at the " +
                            $"default containment channel. A notification is sent to the major notifications channel.\n\n" +
                            $"`disableAntiSpam`: Disables the server's anti spam module.\n\n" +
                            $"`antiSpamIgnore <array of channel ids>`: Tells the anti spam module to ignore the specified channels.\n\n" +
                            $"`antiSpamReset`: Tells the anti spam to no longer ignore any channels in the server.\n\n" +
                            $"`deleteProfile`: Deletes the server profile of the server.\n\n" +
                            $"`updateServerFields`: Adds new and or removes old server profile's data fields.\n\n" +
                            $"`addTimedReminder <name> <content> <repeat> <type> <date>`: Adds a timed reminder which goes off either at a certain date with an option to repeat. In the name and content use \"\\_\" for spaces, there are 3 options for `<date>`, `-r`, `-t` and `-e`. `-r` Adds day-hour-minute amount of time to the current date, in that order and format with numbers. `-t` Works with specific day-hour system, hour is 0-23 and for the day insert the first two letters of the day. `-e` Sets a timer for a very specific date in month-day-hour format. This type of reminder does not repeat even if told to.\n\n" +
                            $"`removeTimedReminder`: Deletes a registered timed reminder.\n\n" +
                            $"`listTimedReminders`: Responds in a list of all the registered timed reminders.\n\n" +
                            $"`setCustomWelcome <welcome_message> <role_id> <channel_id>`: Sets a custom welcome message for the server. The bot will send the given `<welcome_message>` in the channel with id `<channel_id>` and assign the user the role with id `<role_id>`. If you want the bot to mention the user write `MENTION` in the welcome message, the bot will replace it with a user mention. If you don't want the bot to assign a role leave the `<role_id>` as 0.\n\n" +
                            $"`disableCustomWelcome`: Removes the custom welcome message for the server if it exists.\n\n" +
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
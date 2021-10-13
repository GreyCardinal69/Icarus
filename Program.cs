using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Collections.Generic;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using GreyCrammedContainer;

using Icarus.Modules.Other;
using Icarus.Modules.Profiles;
using Icarus.Modules.Logs;
using DSharpPlus.Entities;
using System.Linq;
using System.IO;

namespace Icarus
{
    class Program
    {
        public static string Token;
        public static ulong OwnerID;
        public static Program Core;
        
        public DiscordClient Client { get; private set; }
        public CommandsNextConfiguration CommandsNextConfig { get; private set; }
        public CommandsNextExtension Commands { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }

        public List<ServerProfile> EnabledLogs = new();
        public List<ServerProfile> ServerProfiles = new();
        public List<ulong> RegisteredServerIds = new();
        public DateTimeOffset BotStartUpStamp;

        private readonly EventId BotEventId = new( 42, "Bot-Ex03" );

        static void Main ( string[] args )
        {
            Core = new Program();

            if (GetOperatingSystem() == OSPlatform.Windows)
            {
                Console.WindowWidth = 140;
                Console.WindowHeight = 30;
            }
            Core.BotStartUpStamp = DateTimeOffset.Now;
            Core.RunBotAsync().GetAwaiter().GetResult();
        }

        public static OSPlatform GetOperatingSystem ()
        {
            if (RuntimeInformation.IsOSPlatform( OSPlatform.OSX ))
            {
                return OSPlatform.OSX;
            }

            if (RuntimeInformation.IsOSPlatform( OSPlatform.Linux ))
            {
                return OSPlatform.Linux;
            }

            if (RuntimeInformation.IsOSPlatform( OSPlatform.Windows ))
            {
                return OSPlatform.Windows;
            }

            return OSPlatform.Windows;
        }

        public async Task RunBotAsync ()
        {
            Config Info = GccConverter.Deserialize<Config>( AppDomain.CurrentDomain.BaseDirectory + @"Config.gcc" );
            List<string> Profiles = HelperFuncs.GetAllFilesFromFolder( AppDomain.CurrentDomain.BaseDirectory + @"ServerProfiles\", false );

            foreach (var prof in Profiles)
            {
                ServerProfile Profile = GccConverter.Deserialize<ServerProfile>( prof );
                ServerProfiles.Add( Profile );
                RegisteredServerIds.Add( Profile.ID );
            }
            EnabledLogs = ServerProfiles.Where( X => X.LogConfig.LoggingEnabled ).ToList();

            Program.Token = Info.Token;
            Program.OwnerID = Info.OwnerID;
            var cfg = new DiscordConfiguration
            {
                Intents = DiscordIntents.AllUnprivileged
                    .AddIntent(DiscordIntents.GuildInvites)
                    .AddIntent(DiscordIntents.GuildMembers),
                Token = Info.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Information
            };
            this.Client = new DiscordClient( cfg );
            this.Client.Ready += this.Client_Ready;
            this.Client.GuildAvailable += this.Client_GuildAvailable;
            this.Client.ClientErrored += this.Client_ClientError;
            this.Client.UseInteractivity( new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.Ignore,
                Timeout = TimeSpan.FromMinutes( 2 )
            });
            CommandsNextConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new[] {
                    Info.Prefix
                },
                EnableDms = true,
                EnableMentionPrefix = true,
                IgnoreExtraArguments = true,
                EnableDefaultHelp = false
            };
            Commands = this.Client.UseCommandsNext( CommandsNextConfig );


            // Profiles
            Commands.RegisterCommands<ServProgManagement>();

            // Logging
            Commands.RegisterCommands<LogManagement>();

            // Other
            Commands.RegisterCommands<Help>();
            Commands.RegisterCommands<GeneralCommands>();

            // Events
            Client.GuildMemberRemoved += Event_GuildMemberRemoved;
            Client.GuildMemberAdded += Event_GuildMemberAdded;
            Client.GuildBanRemoved += Event_GuildBanRemoved;
            Client.GuildBanAdded += Event_GuildBanAdded;
            Client.GuildRoleCreated += Event_GuildRoleCreated;
            Client.GuildRoleUpdated += Event_GuildRoleUpdated;
            Client.GuildRoleDeleted += Event_GuildRoleDeleted;
            Client.MessageReactionsCleared += Event_MessageReactionsCleared;
            Client.MessageReactionRemoved += Event_MessageReactionRemoved;
            Client.MessageReactionAdded += Event_MessageReactionAdded;
            Client.MessagesBulkDeleted += Event_MessagesBulkDeleted;
            Client.MessageDeleted += Event_MessageDeleted;
            Client.MessageUpdated += Event_MessageUpdated;
            Client.InviteDeleted += Event_InviteDeleted;
            Client.InviteCreated += Event_InviteCreated;
            Client.ChannelUpdated += Event_ChannelUpdated;
            Client.ChannelCreated += Event_ChannelCreated;
            Client.ChannelDeleted += Event_ChannelDeleted;

            await this.Client.ConnectAsync();
            await Task.Delay( -1 );
        }

        private async Task Event_MessageDeleted ( DiscordClient sender, MessageDeleteEventArgs e )
        {
            if (e.Message.Timestamp < Core.BotStartUpStamp)
            {
                return;
            }
            for (int i = 0; i < EnabledLogs.Count; i++)
            {
                if (e.Guild.Id == EnabledLogs[i].ID)
                {
                    if (EnabledLogs[i].LogConfig.LoggingEnabled && EnabledLogs[i].LogConfig.MessageDeleted)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**Message Deleted**",
                            Color = DiscordColor.Red,
                            Author = new DiscordEmbedBuilder.EmbedAuthor
                            {
                                Name = e.Message.Author.Username + "#" + e.Message.Author.Discriminator,
                                IconUrl = e.Message.Author.AvatarUrl
                            },
                            Description = 
                            $"\n\n**The deleted message was:** " + e.Message.Content + "\n" +
                            $"\n **Message deleted at:** {e.Channel.Mention} \n\n" +
                            "```cs\nThe user's ID is: " + e.Message.Author.Id + "\n" +
                            "The deleted message's ID was: " + e.Message.Id + "\n" +
                            "The Channel's ID is: " + e.Channel.Id + "```",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( EnabledLogs[i].LogConfig.LogChannel ).SendMessageAsync(embed);
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        private async Task Event_InviteDeleted ( DiscordClient sender, InviteDeleteEventArgs e )
        {
            for (int i = 0; i < EnabledLogs.Count; i++)
            {
                if (e.Guild.Id == EnabledLogs[i].ID)
                {
                    if (EnabledLogs[i].LogConfig.LoggingEnabled && EnabledLogs[i].LogConfig.InviteDeleted)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**Invite Deleted**\n\n\n",
                            Color = DiscordColor.IndianRed,
                            Description = 
                            "**The invite was**\n " + e.Invite + "\n\n" +
                            $"**The invite had:**\n " + e.Invite.MaxUses + $" max uses, and {e.Invite.Uses} total uses. \n\n" +
                            $"**The invite was created by** {e.Invite.Inviter.Mention} at: {e.Invite.CreatedAt.UtcDateTime}\n\n",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( EnabledLogs[i].LogConfig.LogChannel ).SendMessageAsync( embed );
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        private async Task Event_InviteCreated ( DiscordClient sender, InviteCreateEventArgs e )
        {
            for (int i = 0; i < EnabledLogs.Count; i++)
            {
                if (e.Guild.Id == EnabledLogs[i].ID)
                {
                    if (EnabledLogs[i].LogConfig.LoggingEnabled && EnabledLogs[i].LogConfig.InviteCreated)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**Invite Created**\n\n\n",
                            Color = DiscordColor.Wheat,
                            Description =
                            "**The invite link is**\n " + e.Invite + "\n\n" +
                            $"**The invite has:**\n " + e.Invite.MaxUses + $" max uses, and {e.Invite.Uses} total uses. \n\n" +
                            $"**The invite was created by** {e.Invite.Inviter.Mention} at: {e.Invite.CreatedAt.UtcDateTime}\n\n",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( EnabledLogs[i].LogConfig.LogChannel ).SendMessageAsync( embed );
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        private async Task Event_ChannelUpdated ( DiscordClient sender, ChannelUpdateEventArgs e )
        {
            for (int i = 0; i < EnabledLogs.Count; i++)
            {
                if (e.Guild.Id == EnabledLogs[i].ID)
                {
                    if (EnabledLogs[i].LogConfig.LoggingEnabled && EnabledLogs[i].LogConfig.ChannelUpdated)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**Channel Updated**",
                            Color = DiscordColor.Wheat,
                            Description =
                            $"**The channel's name was** {e.ChannelBefore.Name}\n\n" +
                            $"**The channel's new name is** {e.ChannelAfter.Name}\n\n" +
                            $"**Is the channel now marked as nsfw?** {e.ChannelAfter.IsNSFW}\n\n" +
                            $"**The channel's new type is** {e.ChannelAfter.Type}\n\n" +
                            $"**The channel's new topic is** {e.ChannelAfter.Topic}\n\n" +
                            $"**The channel's new category is** {e.ChannelAfter.Parent.Name}\n\n" +
                            "```cs\nThe Channel's ID is: " + e.ChannelAfter.Id + "\n" +
                            $"The channel was created at (utc): {e.ChannelBefore.CreationTimestamp.UtcDateTime}```",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( EnabledLogs[i].LogConfig.LogChannel ).SendMessageAsync( embed );
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        private async Task Event_ChannelDeleted ( DiscordClient sender, ChannelDeleteEventArgs e )
        {
            for (int i = 0; i < EnabledLogs.Count; i++)
            {
                if (e.Guild.Id == EnabledLogs[i].ID)
                {
                    if (EnabledLogs[i].LogConfig.LoggingEnabled && EnabledLogs[i].LogConfig.ChannelDeleted)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**Channel Deleted**",
                            Color = DiscordColor.Red,
                            Description =
                            $"**The channel's name was** {e.Channel.Name}\n\n" +
                            $"**Was the channel marked as nsfw?** {e.Channel.IsNSFW}\n\n" +
                            $"**The channel's type was** {e.Channel.Type}\n\n" +
                            $"**The channel's topic was** {e.Channel.Topic}\n\n" +
                            $"**The channel's category was** {e.Channel.Parent.Name}\n\n" +
                            "```cs\nThe Channel's ID was: " + e.Channel.Id + "\n" +
                            $"The channel was created at (utc): {e.Channel.CreationTimestamp.UtcDateTime}```",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( EnabledLogs[i].LogConfig.LogChannel ).SendMessageAsync( embed );
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        private async Task Event_ChannelCreated ( DiscordClient sender, ChannelCreateEventArgs e )
        {
            for (int i = 0; i < EnabledLogs.Count; i++)
            {
                if (e.Guild.Id == EnabledLogs[i].ID)
                {
                    if (EnabledLogs[i].LogConfig.LoggingEnabled && EnabledLogs[i].LogConfig.ChannelCreated)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**Channel Created**",
                            Color = DiscordColor.Wheat,
                            Description =
                            $"**The channel's name is** {e.Channel.Name}\n\n" +
                            $"**Is the channel marked as nsfw?** {e.Channel.IsNSFW}\n\n" +
                            $"**The channel's type is** {e.Channel.Type}\n\n" +
                            $"**The channel's topic is** {e.Channel.Topic}\n\n" +
                            $"**The channel's category is** {e.Channel.Parent.Name}\n\n" +
                            "```cs\nThe Channel's ID is: " + e.Channel.Id + "\n" +
                            $"The channel was created at (utc): {e.Channel.CreationTimestamp.UtcDateTime}```",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( EnabledLogs[i].LogConfig.LogChannel ).SendMessageAsync( embed );
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        private async Task Event_MessageUpdated ( DiscordClient sender, MessageUpdateEventArgs e )
        {
            if (e.Message.Timestamp < Core.BotStartUpStamp)
            {
                return;
            }
            for (int i = 0; i < EnabledLogs.Count; i++)
            {
                if (e.Guild.Id == EnabledLogs[i].ID)
                {
                    if (EnabledLogs[i].LogConfig.LoggingEnabled && EnabledLogs[i].LogConfig.MessageUpdated)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**Message Updated!**\n\n\n",
                            Color = DiscordColor.Gold,
                            Author = new DiscordEmbedBuilder.EmbedAuthor
                            {
                                Name = e.Message.Author.Username + "#" + e.Message.Author.Discriminator,
                                IconUrl = e.Message.Author.AvatarUrl
                            },
                            Description = "**The old message was:**\n " + e.MessageBefore.Content + "\n\n" +
                            "**The new message is:**\n " + e.Message.Content + "\n\n" +
                            $"**Message updated at:** {e.Channel.Mention} \n\n" +
                            $"[Message's Jump Link]({e.Message.JumpLink})\n\n" +
                            "```cs\nThe user's ID is: " + e.Message.Author.Id + "`\n" +
                            "The updated message's ID is: " + e.Message.Id + "\n" +
                            "The Channel's ID is: " + e.Channel.Id + "```",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( EnabledLogs[i].LogConfig.LogChannel ).SendMessageAsync( embed );
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        private async Task Event_MessageReactionAdded ( DiscordClient sender, MessageReactionAddEventArgs e )
        {
            if (e.Message.Timestamp < Core.BotStartUpStamp)
            {
                return;
            }
            for (int i = 0; i < EnabledLogs.Count; i++)
            {
                if (e.Guild.Id == EnabledLogs[i].ID)
                {
                    if (EnabledLogs[i].LogConfig.LoggingEnabled && EnabledLogs[i].LogConfig.MessageReactionAdded)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**Message Reaction Added**",
                            Color = DiscordColor.Wheat,
                            Author = new DiscordEmbedBuilder.EmbedAuthor
                            {
                                Name = e.Message.Author.Username + "#" + e.Message.Author.Discriminator,
                                IconUrl = e.Message.Author.AvatarUrl
                            },
                            Description =
                            $"\n\n**The added reaction is:** " + e.Emoji + "\n" +
                            $"\n[The reaction was added to:]({e.Message.JumpLink}) \n\n" +
                            "```cs\nThe user's ID is: " + e.Message.Author.Id + "\n" +
                            "The Channel's ID is: " + e.Channel.Id + "```",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( EnabledLogs[i].LogConfig.LogChannel ).SendMessageAsync( embed );
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        private async Task Event_MessageReactionsCleared ( DiscordClient sender, MessageReactionsClearEventArgs e )
        {
            if (e.Message.Timestamp < Core.BotStartUpStamp)
            {
                return;
            }
            for (int i = 0; i < EnabledLogs.Count; i++)
            {
                if (e.Guild.Id == EnabledLogs[i].ID)
                {
                    if (EnabledLogs[i].LogConfig.LoggingEnabled && EnabledLogs[i].LogConfig.MessageReactionsCleared)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**Message Reactions Cleared**",
                            Color = DiscordColor.IndianRed,
                            Description = $"\n[The reactions were added to:]({e.Message.JumpLink}) \n\n",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( EnabledLogs[i].LogConfig.LogChannel ).SendMessageAsync( embed );
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        private async Task Event_MessagesBulkDeleted ( DiscordClient sender, MessageBulkDeleteEventArgs e )
        {
            for (int i = 0; i < EnabledLogs.Count; i++)
            {
                if (e.Guild.Id == EnabledLogs[i].ID)
                {
                    if (EnabledLogs[i].LogConfig.LoggingEnabled && EnabledLogs[i].LogConfig.MessagesBulkDeleted)
                    {
                        string TempPath = AppDomain.CurrentDomain.BaseDirectory + "Temp.txt";
                        await File.WriteAllTextAsync( TempPath, string.Join("\n", e.Messages.Select(X => X.Content).Reverse().ToArray()));
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**Messages Purged**\n\n\n",
                            Color = DiscordColor.DarkRed,
                            Author = new DiscordEmbedBuilder.EmbedAuthor
                            {
                                IconUrl = e.Guild.IconUrl
                            },
                            Description = $"\n {e.Messages.Count} messages were purged.\n\n" +
                            "The messages were deleted at: " + e.Channel.Mention + "\n\n" +
                            "Providing a file with all the deleted messages below.",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( EnabledLogs[i].LogConfig.LogChannel ).SendMessageAsync( embed );
                        using (var fs = new FileStream( TempPath, FileMode.Open, FileAccess.Read ))
                        {
                            await new DiscordMessageBuilder()
                                      .WithFile( "Purged_Messages.txt", fs )
                                           .SendAsync( e.Guild.GetChannel( EnabledLogs[i].LogConfig.LogChannel ) );
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        private async Task Event_MessageReactionRemoved ( DiscordClient sender, MessageReactionRemoveEventArgs e )
        {
            if (e.Message.Timestamp < Core.BotStartUpStamp)
            {
                return;
            }
            for (int i = 0; i < EnabledLogs.Count; i++)
            {
                if (e.Guild.Id == EnabledLogs[i].ID)
                {
                    if (EnabledLogs[i].LogConfig.LoggingEnabled && EnabledLogs[i].LogConfig.MessageReactionRemoved)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**Message Reaction Removed**",
                            Color = DiscordColor.IndianRed,
                            Author = new DiscordEmbedBuilder.EmbedAuthor
                            {
                                Name = e.Message.Author.Username + "#" + e.Message.Author.Discriminator,
                                IconUrl = e.Message.Author.AvatarUrl
                            },
                            Description =
                            $"\n\n**The removed reaction is:** " + e.Emoji + "\n" +
                            $"\n[The reaction was removed from:]({e.Message.JumpLink}) \n\n" +
                            "```cs\nThe user's ID is: " + e.Message.Author.Id + "\n" +
                            "The Channel's ID is: " + e.Channel.Id + "```",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( EnabledLogs[i].LogConfig.LogChannel ).SendMessageAsync( embed );
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        private async Task Event_GuildRoleDeleted ( DiscordClient sender, GuildRoleDeleteEventArgs e )
        {
            for (int i = 0; i < EnabledLogs.Count; i++)
            {
                if (e.Guild.Id == EnabledLogs[i].ID)
                {
                    if (EnabledLogs[i].LogConfig.LoggingEnabled && EnabledLogs[i].LogConfig.GuildRoleDeleted)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**Role Deleted**",
                            Color = DiscordColor.Red,
                            Description =
                            $"**The role's name was:** {e.Role.Name}\n\n" +
                            $"**The role's tags were:** {e.Role.Tags}\n\n" +
                            $"**Was the role mentionable?** {e.Role.IsMentionable}\n\n" +
                            $"**The role's color was:** {e.Role.Color}\n\n" +
                            $"```cs\nThe role's id was: {e.Role.Id}\n" +
                            $"The role was created at: {e.Role.CreationTimestamp.UtcDateTime}\n" +
                            $"The role was deleted at: {DateTime.UtcNow}```",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( EnabledLogs[i].LogConfig.LogChannel ).SendMessageAsync( embed );
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        private async Task Event_GuildRoleUpdated ( DiscordClient sender, GuildRoleUpdateEventArgs e )
        {
            for (int i = 0; i < EnabledLogs.Count; i++)
            {
                if (e.Guild.Id == EnabledLogs[i].ID)
                {
                    if (EnabledLogs[i].LogConfig.LoggingEnabled && EnabledLogs[i].LogConfig.GuildRoleUpdated)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**Role Updated**",
                            Color = DiscordColor.Wheat,
                            Description =
                            $"**The role' name change from-to:** {e.RoleBefore.Name} -> {e.RoleAfter.Name}\n\n" +
                            $"**The role's tags changed from-to:** {e.RoleBefore.Tags} -> {e.RoleAfter.Tags}\n\n" +
                            $"**Is the role mentionable?** {e.RoleAfter.IsMentionable}\n\n" +
                            $"**The role's color changed from-to:** {e.RoleBefore.Color} -> {e.RoleAfter.Color}\n\n" +
                            $"```cs\nThe role's id is: {e.RoleAfter.Id}\n" +
                            $"The role was created at: {e.RoleAfter.CreationTimestamp.UtcDateTime}```",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( EnabledLogs[i].LogConfig.LogChannel ).SendMessageAsync( embed );
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        private async Task Event_GuildRoleCreated ( DiscordClient sender, GuildRoleCreateEventArgs e )
        {
            for (int i = 0; i < EnabledLogs.Count; i++)
            {
                if (e.Guild.Id == EnabledLogs[i].ID)
                {
                    if (EnabledLogs[i].LogConfig.LoggingEnabled && EnabledLogs[i].LogConfig.GuildRoleCreated)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**Role Created**",
                            Color = DiscordColor.Wheat,
                            Description =
                            $"**The role's name is:** {e.Role.Name}\n\n"+
                            $"**The role's tags are:** {e.Role.Tags}\n\n"+
                            $"**Is the role mentionable?** {e.Role.IsMentionable}\n\n"+
                            $"**The role's color is:** {e.Role.Color}\n\n"+                          
                            $"```cs\nThe role's id is: {e.Role.Id}\n"+
                            $"The role was created at: {e.Role.CreationTimestamp.UtcDateTime}```",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( EnabledLogs[i].LogConfig.LogChannel ).SendMessageAsync( embed );
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        private async Task Event_GuildBanAdded ( DiscordClient sender, GuildBanAddEventArgs e )
        {
            for (int i = 0; i < EnabledLogs.Count; i++)
            {
                if (e.Guild.Id == EnabledLogs[i].ID)
                {
                    if (EnabledLogs[i].LogConfig.LoggingEnabled && EnabledLogs[i].LogConfig.GuildBanAdded)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**User Banned**",
                            Color = DiscordColor.Red,
                            Author = new DiscordEmbedBuilder.EmbedAuthor
                            {
                                Name = e.Member.Username + "#" + e.Member.Discriminator,
                                IconUrl = e.Member.AvatarUrl
                            },
                            Description =
                            $"**The banned user is:** {e.Member.Mention}\n\n"+
                            $"**The user's roles were:** {string.Join( ", ", e.Member.Roles.Select( X => X.Mention ).ToArray() )}" + "\n\n" +
                            $"**The user joined at:** {e.Member.JoinedAt.UtcDateTime}" + "\n\n" +
                            $"**The user's creation date is**: {e.Member.CreationTimestamp.UtcDateTime}" + "\n\n" +
                            "```cs\nThe user's ID is: " + e.Member.Id + "```",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( EnabledLogs[i].LogConfig.LogChannel ).SendMessageAsync( embed );
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        private async Task Event_GuildBanRemoved ( DiscordClient sender, GuildBanRemoveEventArgs e )
        {
            for (int i = 0; i < EnabledLogs.Count; i++)
            {
                if (e.Guild.Id == EnabledLogs[i].ID)
                {
                    if (EnabledLogs[i].LogConfig.LoggingEnabled && EnabledLogs[i].LogConfig.GuildBanAdded)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**User Unbanned**",
                            Color = DiscordColor.SpringGreen,
                            Author = new DiscordEmbedBuilder.EmbedAuthor
                            {
                                Name = e.Member.Username + "#" + e.Member.Discriminator,
                                IconUrl = e.Member.AvatarUrl
                            },
                            Description =
                            $"**The unbanned user is:** {e.Member.Mention}\n\n" +
                            $"**The user's creation date is**: {e.Member.CreationTimestamp.UtcDateTime}" + "\n\n" +
                            "```cs\nThe user's ID is: " + e.Member.Id + "```",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( EnabledLogs[i].LogConfig.LogChannel ).SendMessageAsync( embed );
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        private async Task Event_GuildMemberAdded ( DiscordClient sender, GuildMemberAddEventArgs e )
        {
            for (int i = 0; i < EnabledLogs.Count; i++)
            {
                if (e.Guild.Id == EnabledLogs[i].ID)
                {
                    if (EnabledLogs[i].LogConfig.LoggingEnabled && EnabledLogs[i].LogConfig.GuildMemberAdded)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**User Joined**",
                            Color = DiscordColor.SpringGreen,
                            Author = new DiscordEmbedBuilder.EmbedAuthor
                            {
                                Name = e.Member.Username + "#" + e.Member.Discriminator,
                                IconUrl = e.Member.AvatarUrl
                            },
                            Description =
                            $"\n **The user joined at:** {e.Member.JoinedAt.UtcDateTime}" + "\n\n" +
                            $"**The user's creation date is**: {e.Member.CreationTimestamp.UtcDateTime}" + "\n\n" +
                            "```cs\nThe user's ID is: " + e.Member.Id + "```",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( EnabledLogs[i].LogConfig.LogChannel ).SendMessageAsync( embed );
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        private async Task Event_GuildMemberRemoved ( DiscordClient sender, GuildMemberRemoveEventArgs e )
        {
            for (int i = 0; i < EnabledLogs.Count; i++)
            {
                if (e.Guild.Id == EnabledLogs[i].ID)
                {
                    if (EnabledLogs[i].LogConfig.LoggingEnabled && EnabledLogs[i].LogConfig.GuildMemberRemoved)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**User Removed**",
                            Color = DiscordColor.Red,
                            Author = new DiscordEmbedBuilder.EmbedAuthor
                            {
                                Name = e.Member.Username + "#" + e.Member.Discriminator,
                                IconUrl = e.Member.AvatarUrl
                            },
                            Description =
                            $"\n **The user joined at:** {e.Member.JoinedAt.UtcDateTime}" + "\n\n" +
                            $"**The user's creation date is**: {e.Member.CreationTimestamp.UtcDateTime}" + "\n\n" +
                            $"**The user's roles were:** {string.Join(", ", e.Member.Roles.Select(X => X.Mention ).ToArray())}"+ "\n\n" +
                            "```cs\nThe user's ID is: " + e.Member.Id + "\n",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( EnabledLogs[i].LogConfig.LogChannel ).SendMessageAsync( embed );
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        private Task Client_Ready ( DiscordClient sender, ReadyEventArgs e )
        {
            sender.Logger.LogInformation( BotEventId, "Client is ready to process events." );
            return Task.CompletedTask;
        }

        private Task Client_GuildAvailable ( DiscordClient sender, GuildCreateEventArgs e )
        {
            sender.Logger.LogInformation( BotEventId, $"Guild available: {e.Guild.Name}" );
            return Task.CompletedTask;
        }

        private Task Client_ClientError ( DiscordClient sender, ClientErrorEventArgs e )
        {
            sender.Logger.LogError( BotEventId, e.Exception, "Exception occured" );
            return Task.CompletedTask;
        }
    }
}
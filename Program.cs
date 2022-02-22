using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Timers;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Icarus.Modules.Other;
using Icarus.Modules;
using Icarus.Modules.Logs;
using Icarus.Modules.Servers;
using Icarus.Modules.Isolation;

namespace Icarus
{
    class Program
    {
        public static Program Core { get; private set; }

        public DiscordClient Client { get; private set; }
        public CommandsNextConfiguration CommandsNextConfig { get; private set; }
        public CommandsNextExtension Commands { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }
        public DateTimeOffset BotStartUpStamp { get; private set; }
        public ulong OwnerId { get; private set; }

        public List<ulong> RegisteredServerIds = new();
        public List<ServerProfile> ServerProfiles = new();

        private string _token;
        private System.Timers.Timer _entryCheckTimer;
        private readonly EventId BotEventId = new( 1488, "Bot-Ex1488" );

        private static void Main ( string[] args )
        {
            Core = new Program();

            if (GetOperatingSystem() == OSPlatform.Windows)
            {
                #pragma warning disable CA1416
                Console.WindowWidth = 140;
                Console.WindowHeight = 30;
                #pragma warning restore CA1416
            }

            Core._entryCheckTimer = new(600000);
            Core._entryCheckTimer.Elapsed += async ( sender, e ) => await HandleTimer();
            Core._entryCheckTimer.Start();
            Core._entryCheckTimer.AutoReset = true;
            Core._entryCheckTimer.Enabled = true;
            Core.BotStartUpStamp = DateTimeOffset.Now;
            Core.RunBotAsync().GetAwaiter().GetResult();
        }

        private static async Task<Task> HandleTimer ()
        {
            for (int i = 0; i < Core.ServerProfiles.Count; i++)
            {
                for (int w = 0; w < Core.ServerProfiles[i].Entries.Count; w++)
                {
                    if (DateTime.UtcNow > Core.ServerProfiles[i].Entries[w].ReleaseDate)
                    {
                        await IsolationManagement.ReleaseEntry( Core.ServerProfiles[i], Core.ServerProfiles[i].Entries[w] );
                    }
                }
            }
            Console.WriteLine( $"Completed 10 minute server entries check [{DateTime.UtcNow}]." );
            return Task.CompletedTask;
        }

        private static OSPlatform GetOperatingSystem ()
        {
            if (RuntimeInformation.IsOSPlatform( OSPlatform.OSX ))
                return OSPlatform.OSX;
            if (RuntimeInformation.IsOSPlatform( OSPlatform.Linux ))
                return OSPlatform.Linux;
            if (RuntimeInformation.IsOSPlatform( OSPlatform.Windows ))
                return OSPlatform.Windows;
            return OSPlatform.Windows;
        }

        private void SetToken ( string token )
        {
            this._token = token;
        }

        private async Task RunBotAsync ()
        {
            Config Info = JsonConvert.DeserializeObject<Config>( File.ReadAllText( AppDomain.CurrentDomain.BaseDirectory + @"Config.json" ));

            Core.SetToken( Info.Token );
            Core.OwnerId = Info.OwnerId;

            List<string> Profiles = Helpers.GetAllFilesFromFolder( AppDomain.CurrentDomain.BaseDirectory + @"ServerProfiles\", false );

            foreach (var prof in Profiles)
            {
                ServerProfile Profile = JsonConvert.DeserializeObject<ServerProfile>( File.ReadAllText( prof ) );
                ServerProfiles.Add( Profile );
                RegisteredServerIds.Add( Profile.ID );
            }

            var cfg = new DiscordConfiguration
            {
                Intents =       DiscordIntents.AllUnprivileged
                    .AddIntent( DiscordIntents.GuildInvites )
                    .AddIntent( DiscordIntents.GuildMembers )
                    .AddIntent( DiscordIntents.AllUnprivileged )
                    .AddIntent( DiscordIntents.All ),
                Token = Info.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Information
            };

            this.Client = new DiscordClient( cfg );
            Core.Client = this.Client;

            this.Client.Ready += this.Client_Ready;
            this.Client.GuildAvailable += this.Client_GuildAvailable;
            this.Client.ClientErrored += this.Client_ClientError;
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
            Client.MessageCreated += Event_MessageCreated;
            Client.InviteDeleted += Event_InviteDeleted;
            Client.InviteCreated += Event_InviteCreated;
            Client.ChannelUpdated += Event_ChannelUpdated;
            Client.ChannelCreated += Event_ChannelCreated;
            Client.ChannelDeleted += Event_ChannelDeleted;

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

            Commands.RegisterCommands<GeneralCommands>();
            Commands.RegisterCommands<Help>();

            Commands.RegisterCommands<LogManagement>();
            Commands.RegisterCommands<ServerManagement>();
            Commands.RegisterCommands<IsolationManagement>();

            await this.Client.ConnectAsync();
            await Task.Delay( -1 );
        }

        private async Task Event_MessageCreated ( DiscordClient sender, MessageCreateEventArgs e )
        {
            foreach (var link in Database.ScamLinks)
            {
                if (e.Message.Content.Contains(link))
                {
                    var cmds = Program.Core.Client.GetCommandsNext();
                    var cmd = cmds.FindCommand( "isolate", out var customArgs );
                    customArgs = "[]help. Hunting For Pulsars.";
                    var guild = Program.Core.Client.GetGuildAsync( e.Guild.Id ).Result;
                    var user = guild.GetMemberAsync( e.Author.Id ).Result;
                    var fakeContext = cmds.CreateFakeContext
                        (
                            user,
                            Program.Core.Client.GetChannelAsync( e.Channel.Id ).Result,
                            "isolate", ">",
                            cmd,
                            customArgs
                        );
                    await fakeContext.RespondAsync("found link");
                }
            }
            return;
        }

        private async Task Event_InviteDeleted ( DiscordClient sender, InviteDeleteEventArgs e )
        {
            for (int i = 0; i < ServerProfiles.Count; i++)
            {
                if (e.Guild.Id == ServerProfiles[i].ID)
                {
                    if (ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.InviteDeleted)
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
                        await e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel ).SendMessageAsync( embed );
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
            for (int i = 0; i < ServerProfiles.Count; i++)
            {
                if (e.Guild.Id == ServerProfiles[i].ID)
                {
                    if (ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.InviteCreated)
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
                        await e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel ).SendMessageAsync( embed );
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
            for (int i = 0; i < ServerProfiles.Count; i++)
            {
                if (e.Guild.Id == ServerProfiles[i].ID)
                {
                    if (ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.ChannelUpdated)
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
                        await e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel ).SendMessageAsync( embed );
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
            for (int i = 0; i < ServerProfiles.Count; i++)
            {
                if (e.Guild.Id == ServerProfiles[i].ID)
                {
                    if (ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.ChannelDeleted)
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
                        await e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel ).SendMessageAsync( embed );
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
            for (int i = 0; i < ServerProfiles.Count; i++)
            {
                if (e.Guild.Id == ServerProfiles[i].ID)
                {
                    if (ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.ChannelCreated)
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
                        await e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel ).SendMessageAsync( embed );
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
            for (int i = 0; i < ServerProfiles.Count; i++)
            {
                if (e.Guild.Id == ServerProfiles[i].ID)
                {
                    if (ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.MessageUpdated)
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
                            "```cs\nThe user's ID is: " + e.Message.Author.Id + "\n" +
                            "The updated message's ID is: " + e.Message.Id + "\n" +
                            "The Channel's ID is: " + e.Channel.Id + "```",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel ).SendMessageAsync( embed );
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
            for (int i = 0; i < ServerProfiles.Count; i++)
            {
                if (e.Guild.Id == ServerProfiles[i].ID)
                {
                    if (ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.MessageReactionAdded)
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
                        await e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel ).SendMessageAsync( embed );
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
            for (int i = 0; i < ServerProfiles.Count; i++)
            {
                if (e.Guild.Id == ServerProfiles[i].ID)
                {
                    if (ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.MessageReactionsCleared)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**Message Reactions Cleared**",
                            Color = DiscordColor.IndianRed,
                            Description = $"\n[The reactions were added to:]({e.Message.JumpLink}) \n\n",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel ).SendMessageAsync( embed );
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
            for (int i = 0; i < ServerProfiles.Count; i++)
            {
                if (e.Guild.Id == ServerProfiles[i].ID)
                {
                    if (ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.MessagesBulkDeleted)
                    {
                        string TempPath = AppDomain.CurrentDomain.BaseDirectory + "Temp.txt";
                        await File.WriteAllTextAsync( TempPath, string.Join( "\n", e.Messages.Select( X => X.Content ).Reverse().ToArray() ) );
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
                        await e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel ).SendMessageAsync( embed );
                        using (var fs = new FileStream( TempPath, FileMode.Open, FileAccess.Read ))
                        {
                            await new DiscordMessageBuilder()
                                      .WithFile( "Purged_Messages.txt", fs )
                                           .SendAsync( e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel ) );
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
            for (int i = 0; i < ServerProfiles.Count; i++)
            {
                if (e.Guild.Id == ServerProfiles[i].ID)
                {
                    if (ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.MessageReactionRemoved)
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
                        await e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel ).SendMessageAsync( embed );
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
            for (int i = 0; i < ServerProfiles.Count; i++)
            {
                if (e.Guild.Id == ServerProfiles[i].ID)
                {
                    if (ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.GuildRoleDeleted)
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
                        await e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel ).SendMessageAsync( embed );
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
            for (int i = 0; i < ServerProfiles.Count; i++)
            {
                if (e.Guild.Id == ServerProfiles[i].ID)
                {
                    if (ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.GuildRoleUpdated)
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
                        await e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel ).SendMessageAsync( embed );
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
            for (int i = 0; i < ServerProfiles.Count; i++)
            {
                if (e.Guild.Id == ServerProfiles[i].ID)
                {
                    if (ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.GuildRoleCreated)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "**Role Created**",
                            Color = DiscordColor.Wheat,
                            Description =
                            $"**The role's name is:** {e.Role.Name}\n\n" +
                            $"**The role's tags are:** {e.Role.Tags}\n\n" +
                            $"**Is the role mentionable?** {e.Role.IsMentionable}\n\n" +
                            $"**The role's color is:** {e.Role.Color}\n\n" +
                            $"```cs\nThe role's id is: {e.Role.Id}\n" +
                            $"The role was created at: {e.Role.CreationTimestamp.UtcDateTime}```",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel ).SendMessageAsync( embed );
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
            for (int i = 0; i < ServerProfiles.Count; i++)
            {
                if (e.Guild.Id == ServerProfiles[i].ID)
                {
                    if (ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.GuildBanAdded)
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
                            $"**The banned user is:** {e.Member.Mention}\n\n" +
                            $"**The user's roles were:** {string.Join( ", ", e.Member.Roles.Select( X => X.Mention ).ToArray() )}" + "\n\n" +
                            $"**The user joined at:** {e.Member.JoinedAt.UtcDateTime}" + "\n\n" +
                            $"**The user's creation date is**: {e.Member.CreationTimestamp.UtcDateTime}" + "\n\n" +
                            "```cs\nThe user's ID is: " + e.Member.Id + "```",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel ).SendMessageAsync( embed );
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
            for (int i = 0; i < ServerProfiles.Count; i++)
            {
                if (e.Guild.Id == ServerProfiles[i].ID)
                {
                    if (ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.GuildBanAdded)
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
                        await e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel ).SendMessageAsync( embed );
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
            if (e.Guild.Id == 740528944129900565)
            {
                await e.Member.GrantRoleAsync(e.Guild.GetRole( 740557101843087441 ) );
            }
            for (int i = 0; i < ServerProfiles.Count; i++)
            {
                if (e.Guild.Id == ServerProfiles[i].ID)
                {
                    if (ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.GuildMemberAdded)
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
                        await e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel ).SendMessageAsync( embed );
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
            for (int i = 0; i < ServerProfiles.Count; i++)
            {
                if (e.Guild.Id == ServerProfiles[i].ID)
                {
                    if (ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.GuildMemberRemoved)
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
                            $"**The user's roles were:** {string.Join( ", ", e.Member.Roles.Select( X => X.Mention ).ToArray() )}" + "\n\n" +
                            "```cs\nThe user's ID is: " + e.Member.Id + "\n",
                            Timestamp = DateTime.Now,
                        };
                        await e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel ).SendMessageAsync( embed );
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        private async Task Event_MessageDeleted ( DiscordClient sender, MessageDeleteEventArgs e )
        {
            if (e.Message.Timestamp < Core.BotStartUpStamp)
            {
                return;
            }
            for (int i = 0; i < ServerProfiles.Count; i++)
            {
                if (e.Guild.Id == ServerProfiles[i].ID)
                {
                    if (ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.MessageDeleted)
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
                        await e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel ).SendMessageAsync( embed );
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
            Core.Client.UpdateStatusAsync( new DiscordActivity( "You", ActivityType.Watching ), UserStatus.DoNotDisturb, DateTimeOffset.UtcNow );
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
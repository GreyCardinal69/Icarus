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
using Icarus.Modules.Profiles;

namespace Icarus
{
    class Program
    {
        public static Program Core { get; private set; }

        public DiscordClient Client { get; private set; }
        public CommandsNextConfiguration CommandsNextConfig { get; private set; }
        public CommandsNextExtension Commands { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }
        public DateTime BotStartUpStamp { get; private set; }
        public ulong OwnerId { get; private set; }

        public List<ulong> RegisteredServerIds = new();
        public List<ServerProfile> ServerProfiles = new();
        private List<(ulong, int)> _temporaryMessageCounter = new();

        private Timer _entryCheckTimer;
        private Timer _antiSpamTimer;
        private readonly EventId BotEventId = new( 1458, "Bot-Ex1458" );

        private static void Main ( string[] args )
        {
            Core = new();

            List<string> Profiles = Helpers.GetAllFilesFromFolder( AppDomain.CurrentDomain.BaseDirectory + @"ServerProfiles\", false );

            foreach (var prof in Profiles)
            {
                ServerProfile profile = JsonConvert.DeserializeObject<ServerProfile>( File.ReadAllText( prof ) );
                Core.ServerProfiles.Add( profile );
                Core.RegisteredServerIds.Add( profile.ID );
            }

            if (GetOperatingSystem() == OSPlatform.Windows)
            {
#pragma warning disable CA1416
                Console.WindowWidth = 140;
                Console.WindowHeight = 30;
#pragma warning restore CA1416
            }

            Core._entryCheckTimer = new( 600000 );
            Core._entryCheckTimer.Elapsed += async ( sender, e ) => await HandleTimer();
            Core._entryCheckTimer.Start();
            Core._entryCheckTimer.AutoReset = true;
            Core._entryCheckTimer.Enabled = true;

            Core._antiSpamTimer = new( 20000 );
            Core._antiSpamTimer.Elapsed += async ( sender, e ) => await ResetMessageCache();
            Core._antiSpamTimer.Start();
            Core._antiSpamTimer.AutoReset = true;
            Core._antiSpamTimer.Enabled = true;

            Core.BotStartUpStamp = DateTime.Now;
            Core.RunBotAsync().GetAwaiter().GetResult();
        }

        #pragma warning disable CS1998
        private static async Task<Task> ResetMessageCache ()
        {
            Core._temporaryMessageCounter.Clear();
            return Task.CompletedTask;
        }
        #pragma warning restore CS1998

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
            Core._temporaryMessageCounter.Clear();
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

        private async Task RunBotAsync ()
        {
            Config info = JsonConvert.DeserializeObject<Config>( File.ReadAllText( AppDomain.CurrentDomain.BaseDirectory + @"Config.json" ) );

            Core.OwnerId = info.OwnerId;

            var cfg = new DiscordConfiguration
            {
                Intents = DiscordIntents.All,
                Token = info.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Information
            };

            Client = new DiscordClient( cfg );
            Core.Client = Client;

            Client.Ready += Client_Ready;
            Client.GuildAvailable += Client_GuildAvailable;
            Client.ClientErrored += Client_ClientError;
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
            //Client.MessagesBulkDeleted += Event_MessagesBulkDeleted;
            Client.MessageDeleted += Event_MessageDeleted;
            Client.MessageUpdated += Event_MessageUpdated;
            Client.MessageCreated += Event_MessageCreated;
            Client.InviteDeleted += Event_InviteDeleted;
            Client.InviteCreated += Event_InviteCreated;
            Client.ChannelUpdated += Event_ChannelUpdated;
            Client.ChannelCreated += Event_ChannelCreated;
            Client.ChannelDeleted += Event_ChannelDeleted;

            Client.UseInteractivity( new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.Ignore,
                Timeout = TimeSpan.FromMinutes( 2 ),
            });

            CommandsNextConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new[] {
                    info.Prefix
                },
                EnableDms = true,
                EnableMentionPrefix = true,
                IgnoreExtraArguments = true,
                EnableDefaultHelp = false
            };

            Commands = Client.UseCommandsNext( CommandsNextConfig );

            Commands.RegisterCommands<GeneralCommands>();
            Commands.RegisterCommands<Help>();

            Commands.RegisterCommands<LogManagement>();
            Commands.RegisterCommands<ServerManagement>();
            Commands.RegisterCommands<IsolationManagement>();
            Commands.RegisterCommands<UserLogging>();

            await Client.ConnectAsync();
            await Task.Delay( -1 );
        }

        private async Task Event_MessageCreated ( DiscordClient sender, MessageCreateEventArgs e )
        {
            if (e.Author.Id == Core.Client.CurrentUser.Id || e.Author.Id == OwnerId)
            {
                return;
            }

            var user = e.Guild.GetMemberAsync( e.Message.Author.Id ).Result;
            var perms = user.Permissions;

            if ( perms.HasPermission(Permissions.Administrator  ) ||
                 perms.HasPermission(Permissions.BanMembers     ) ||
                 perms.HasPermission(Permissions.KickMembers    ) ||
                 perms.HasPermission(Permissions.ManageChannels ) ||
                 perms.HasPermission(Permissions.ManageGuild    ) ||
                 perms.HasPermission(Permissions.ManageMessages ) ||
                 perms.HasPermission(Permissions.ManageRoles    ) ||
                 perms.HasPermission(Permissions.ManageEmojis   ))
            {
                return;
            }

            var profile = ServerProfile.ProfileFromId( e.Guild.Id );

            foreach (var word in profile.WordBlackList)
            {
                if (e.Message.Content.Contains(word))
                {
                    var cmds = Program.Core.Client.GetCommandsNext();
                    var cmd = cmds.FindCommand( "isolate", out var customArgs );
                    customArgs = "[]help. Hunting For Pulsars.";
                    var guild = Program.Core.Client.GetGuildAsync( e.Guild.Id ).Result;
                    await cmds.CreateFakeContext( user, guild.GetChannel( profile.LogConfig.MajorNotificationsChannelId ), "isolate", ">", cmd, customArgs )
                        .RespondAsync( $"The following user {e.Author.Mention} mentioned {word} in {e.Channel.Mention}, message link: {e.Message.JumpLink}." );
                    break;
                }
            }

            if (!profile.AntiSpamIgnored.Contains(e.Channel.Id))
            {
                if (_temporaryMessageCounter.Count <= 0)
                {
                    _temporaryMessageCounter.Add( (e.Author.Id, 1) );
                }

                bool found = false;
                for (int i = 0; i < _temporaryMessageCounter.Count; i++)
                {
                    if (_temporaryMessageCounter[i].Item1 == e.Author.Id)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    _temporaryMessageCounter.Add( (e.Author.Id, 1) );
                }

                for (int i = 0; i < _temporaryMessageCounter.Count; i++)
                {
                    if (_temporaryMessageCounter[i].Item1 == e.Author.Id)
                    {
                        _temporaryMessageCounter[i] = (e.Author.Id, _temporaryMessageCounter[i].Item2 + 1);
                        if (_temporaryMessageCounter[i].Item2 >= profile.AntiSpam.FirstWarning)
                        {
                            var cmds = Program.Core.Client.GetCommandsNext();
                            var cmd = cmds.FindCommand( "isolate", out var customArgs );
                            customArgs = "[]help. Hunting For Pulsars.";
                            var guild = Program.Core.Client.GetGuildAsync( e.Guild.Id ).Result;
                            var fakeContext = cmds.CreateFakeContext(
                                    user,
                                    guild.GetChannel( e.Channel.Id ),
                                    "isolate", ">",
                                    cmd,
                                    customArgs
                            );
                            if (_temporaryMessageCounter[i].Item2 == profile.AntiSpam.FirstWarning)
                            {
                                await fakeContext.RespondAsync( $"{e.Author.Mention} Stop sending messages so quickly." );
                            }
                            else if (_temporaryMessageCounter[i].Item2 == profile.AntiSpam.SecondWarning)
                            {
                                await fakeContext.RespondAsync( $"{e.Author.Mention} Your actions are considered spam." );
                            }
                            else if (_temporaryMessageCounter[i].Item2 == profile.AntiSpam.LastWarning)
                            {
                                await fakeContext.RespondAsync( $"{e.Author.Mention} This is your final warning, calm down." );
                            }
                            else if (_temporaryMessageCounter[i].Item2 > profile.AntiSpam.Limit)
                            {
                                await fakeContext.RespondAsync( $"{e.Author.Mention} You will be isolated now." );
                                var messages = await fakeContext.Channel.GetMessagesAsync( _temporaryMessageCounter[i].Item2 + 4 );
                                await fakeContext.Channel.DeleteMessagesAsync( messages );
                                await user.GrantRoleAsync( guild.GetRole( profile.LogConfig.DefaultContainmentRoleId ) );
                                await cmds.CreateFakeContext( user, guild.GetChannel( profile.LogConfig.MajorNotificationsChannelId ), "isolate", ">", cmd, customArgs )
                                    .RespondAsync(
                                    $"Isolated user {user.Mention} at {guild.GetChannel( profile.LogConfig.DefaultContainmentChannelId ).Mention}. " +
                                    $"The user's actions were considered spam. " +
                                    $"Revoked the following roles from the user: {string.Join( ", ", user.Roles.Select( x => x.Mention ).ToArray() )}."
                                );
                                foreach (var role in user.Roles)
                                {
                                    await user.RevokeRoleAsync( role );
                                }

                                var userP = JsonConvert.DeserializeObject<UserProfile>(
                                     File.ReadAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{e.Guild.Id}UserProfiles\{e.Author.Id}.json" ) );

                                userP.PunishmentEntries.Add( ( DateTime.UtcNow, "User's actions were considered spam." ) );

                                File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{e.Guild.Id}UserProfiles\{e.Author.Id}.json",
                                     JsonConvert.SerializeObject( userP, Formatting.Indented ) );
                            }
                        }
                        break;
                    }
                }
            }

            foreach (var link in Database.ScamLinks)
            {
                if (e.Message.Content.Contains(link))
                {
                    var cmds = Program.Core.Client.GetCommandsNext();
                    var cmd = cmds.FindCommand( "isolate", out var customArgs );
                    customArgs = "[]help. Hunting For Pulsars.";
                    var guild = Program.Core.Client.GetGuildAsync( e.Guild.Id ).Result;
                    var fakeContext = cmds.CreateFakeContext(
                            user,
                            guild.GetChannel( profile.LogConfig.MajorNotificationsChannelId ),
                            "isolate", ">",
                            cmd,
                            customArgs
                    );

                    await user.GrantRoleAsync( guild.GetRole( profile.LogConfig.DefaultContainmentRoleId ) );
                    await fakeContext.RespondAsync(
                        $"Isolated user {user.Mention} at {guild.GetChannel(profile.LogConfig.DefaultContainmentChannelId).Mention}. " +
                        $"The user's message contained a discord scam link, the link was: `{link}`. " +
                        $"Revoked the following roles from the user: {string.Join(", ", user.Roles.Select( x => x.Mention ).ToArray())}."
                    );

                    foreach (var role in user.Roles)
                    {
                        await user.RevokeRoleAsync( role );
                    }

                    var userP = JsonConvert.DeserializeObject<UserProfile>(
                          File.ReadAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{e.Guild.Id}UserProfiles\{e.Author.Id}.json" ) );

                    userP.PunishmentEntries.Add( ( DateTime.UtcNow, "User posted a scam message." ) );

                    File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{e.Guild.Id}UserProfiles\{e.Author.Id}.json",
                         JsonConvert.SerializeObject( userP, Formatting.Indented ) );

                    await e.Message.DeleteAsync();
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

            var profile = ServerProfile.ProfileFromId( e.Guild.Id );

            foreach (var word in profile.WordBlackList)
            {

                var user = e.Guild.GetMemberAsync( e.Message.Author.Id ).Result;
                var perms = user.Permissions;

                if ( perms.HasPermission( Permissions.Administrator  ) ||
                     perms.HasPermission( Permissions.BanMembers     ) ||
                     perms.HasPermission( Permissions.KickMembers    ) ||
                     perms.HasPermission( Permissions.ManageChannels ) ||
                     perms.HasPermission( Permissions.ManageGuild    ) ||
                     perms.HasPermission( Permissions.ManageMessages ) ||
                     perms.HasPermission( Permissions.ManageRoles    ) ||
                     perms.HasPermission( Permissions.ManageEmojis   ))
                {
                    break;
                }

                if (e.Message.Content.Contains( word ))
                {
                    var cmds = Program.Core.Client.GetCommandsNext();
                    var cmd = cmds.FindCommand( "isolate", out var customArgs );
                    customArgs = "[]help. Hunting For Pulsars.";
                    var guild = Program.Core.Client.GetGuildAsync( e.Guild.Id ).Result;
                    await cmds.CreateFakeContext( user, guild.GetChannel( profile.LogConfig.MajorNotificationsChannelId ), "isolate", ">", cmd, customArgs )
                        .RespondAsync(
                        $"The following user {e.Author.Mention} mentioned {word} in {e.Channel.Mention}, message link: {e.Message.JumpLink}.\n" +
                        $"The user mentioned the black-listed word after editing a message."
                    );
                    break; ;
                }
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
                await e.Member.GrantRoleAsync( e.Guild.GetRole( 740557101843087441 ) );
                var main = e.Guild.GetChannel( 740528944641736756 );
                await main.SendMessageAsync( e.Member.Mention + " I am watching you, and welcome." );
            }
            for (int i = 0; i < ServerProfiles.Count; i++)
            {
                if (e.Guild.Id == ServerProfiles[i].ID)
                {
                    if (!File.Exists( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{e.Guild.Id}UserProfiles\{e.Member.Id}.json" ))
                    {
                        var profile = new UserProfile( e.Member.Id, e.Member.Username )
                        {
                            Discriminator = e.Member.Discriminator,
                            CreationDate = e.Member.CreationTimestamp,
                            FirstJoinDate = e.Member.JoinedAt,
                            LocalLanguage = e.Member.Locale
                        };

                        profile.LastJoinDate = DateTime.UtcNow;

                        File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{e.Guild.Id}UserProfiles\{e.Member.Id}.json",
                           JsonConvert.SerializeObject( profile, Formatting.Indented ) );
                    }

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
                    var user = JsonConvert.DeserializeObject<UserProfile>(
                        File.ReadAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{e.Guild.Id}UserProfiles\{e.Member.Id}.json" ) );

                    user.LeaveDate = DateTime.UtcNow;

                    File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{e.Guild.Id}UserProfiles\{e.Member.Id}.json",
                       JsonConvert.SerializeObject( user, Formatting.Indented ) );

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
            //Console.Clear();
            sender.Logger.LogError( BotEventId, e.Exception, "Exception occured" );
            return Task.CompletedTask;
        }
    }
}
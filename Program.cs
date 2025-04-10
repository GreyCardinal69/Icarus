﻿using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Icarus.Modules;
using Icarus.Modules.Isolation;
using Icarus.Modules.Logs;
using Icarus.Modules.Other;
using Icarus.Modules.Profiles;
using Icarus.Modules.Servers;
using Icarus.Modules.ServerSpecific;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Timers;

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

        public List<ulong> RegisteredServerIds = new List<ulong>();
        public List<ServerProfile> ServerProfiles = new List<ServerProfile>();
        private List<(ulong, int)> _temporaryMessageCounter = new List<(ulong, int)>();

        private Timer _entryCheckTimer;
        private Timer _antiSpamTimer;
        private readonly EventId BotEventId = new( 1458, "Bot-Ex1458" );

        private static void Main( string[] args )
        {
            Core = new Program();

            List<string> Profiles = Helpers.GetAllFilesFromFolder( AppDomain.CurrentDomain.BaseDirectory + @"ServerProfiles\", false );

            foreach ( string prof in Profiles )
            {
                ServerProfile profile = JsonConvert.DeserializeObject<ServerProfile>( File.ReadAllText( prof ) );
                Core.ServerProfiles.Add( profile );
                Core.RegisteredServerIds.Add( profile.ID );
            }

            if ( GetOperatingSystem() == OSPlatform.Windows )
            {
                Console.WindowWidth = 140;
                Console.WindowHeight = 30;
            }

            Core._entryCheckTimer = new Timer( 60000 );
            Core._entryCheckTimer.Elapsed += async ( sender, e ) => await HandleTimer();
            Core._entryCheckTimer.Start();
            Core._entryCheckTimer.AutoReset = true;
            Core._entryCheckTimer.Enabled = true;

            Core._antiSpamTimer = new Timer( 20000 );
            Core._antiSpamTimer.Elapsed += async ( sender, e ) => await ResetMessageCache();
            Core._antiSpamTimer.Start();
            Core._antiSpamTimer.AutoReset = true;
            Core._antiSpamTimer.Enabled = true;

            Core.BotStartUpStamp = DateTime.Now;
            Core.RunBotAsync().GetAwaiter().GetResult();
        }

#pragma warning disable CS1998
        private static async Task<Task> ResetMessageCache()
        {
            Core._temporaryMessageCounter.Clear();
            return Task.CompletedTask;
        }
#pragma warning restore CS1998

        private static async Task<Task> HandleTimer()
        {
            for ( int i = 0; i < Core.ServerProfiles.Count; i++ )
            {
                ServerProfile profile = Core.ServerProfiles[i];
                for ( int w = 0; w < profile.Entries.Count; w++ )
                {
                    if ( DateTime.Now > profile.Entries[w].ReleaseDate )
                    {
                        await IsolationManagement.ReleaseEntry( profile, profile.Entries[w] );
                    }
                }

                DateTimeOffset now = DateTimeOffset.UtcNow;
                CommandContext tempContext = Core.CreateCommandContext( profile.ID, profile.LogConfig.MajorNotificationsChannelId );

                for ( int e = 0; e < profile.TimedReminders.Count; e++ )
                {
                    TimedReminder item = profile.TimedReminders[e];
                    if ( item.HasExpired( now ) )
                    {
                        await tempContext.RespondAsync( item.ToString() );
                        if ( !item.Repeat )
                        {
                            profile.TimedReminders.Remove( item );
                            await tempContext.RespondAsync( $"Timed Reminder not set to repeat, removing it from server reminders list." );
                            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{tempContext.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
                        }
                        else
                        {
                            item.UpdateExpDate();
                            await tempContext.RespondAsync( $"Timed Reminder set to repeat, repeating..\n Next time the reminder will go off at <t:{item.ExpDate}>." );
                            File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{tempContext.Guild.Id}.json", JsonConvert.SerializeObject( profile, Formatting.Indented ) );
                        }
                    }
                }
            }

            Core._temporaryMessageCounter.Clear();
            Console.WriteLine( $"Completed minute server entries check [{DateTimeOffset.Now}]." );
            return Task.CompletedTask;
        }

        private static OSPlatform GetOperatingSystem()
        {
            if ( RuntimeInformation.IsOSPlatform( OSPlatform.OSX ) )
                return OSPlatform.OSX;
            if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) )
                return OSPlatform.Linux;
            if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
                return OSPlatform.Windows;
            return OSPlatform.Windows;
        }

        private async Task RunBotAsync()
        {
            Config info = JsonConvert.DeserializeObject<Config>( File.ReadAllText( AppDomain.CurrentDomain.BaseDirectory + @"Config.json" ) );

            Core.OwnerId = info.OwnerId;

            DiscordConfiguration cfg = new DiscordConfiguration
            {
                Intents = DiscordIntents.All,
                Token = info.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Information,
                LogUnknownEvents = false,
                ReconnectIndefinitely = true,
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
            Client.MessagesBulkDeleted += Event_MessagesBulkDeleted;
            Client.MessageDeleted += Event_MessageDeleted;
            Client.MessageUpdated += Event_MessageUpdated;
            Client.MessageCreated += Event_MessageCreated;
            Client.InviteDeleted += Event_InviteDeleted;
            Client.InviteCreated += Event_InviteCreated;
            Client.ChannelUpdated += Event_ChannelUpdated;
            Client.ChannelCreated += Event_ChannelCreated;
            Client.ChannelDeleted += Event_ChannelDeleted;

            SlashCommandsExtension slash = Client.UseSlashCommands();
            slash.RegisterCommands<SlashCommands>();
            // Server specific class, not included in repository.
            slash.RegisterCommands<EventHorizonSlash>();

            Client.UseInteractivity( new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.Ignore,
                Timeout = TimeSpan.FromMinutes( 2 ),
            } );

            CommandsNextConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new[] {
                    info.Prefix
                },
                EnableDms = true,
                EnableMentionPrefix = true,
                IgnoreExtraArguments = true,
                EnableDefaultHelp = false,
            };

            Commands = Client.UseCommandsNext( CommandsNextConfig );

            Commands.RegisterCommands<GeneralCommands>();
            Commands.RegisterCommands<Help>();

            Commands.RegisterCommands<LogManagement>();
            Commands.RegisterCommands<ServerManagement>();
            Commands.RegisterCommands<IsolationManagement>();
            Commands.RegisterCommands<UserLogging>();
            // Server specific class, not included in repository.
            Commands.RegisterCommands<EventHorizon>();

            await Client.ConnectAsync();
            await Task.Delay( -1 );
        }

        private async Task Event_MessagesBulkDeleted( DiscordClient sender, MessageBulkDeleteEventArgs args )
        {
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( ServerProfiles[i].LogConfig.ExcludedChannels.Contains( args.Channel.Id ) )
                    return;
            }
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( args.Guild.Id == ServerProfiles[i].ID )
                {
                    DiscordChannel channel = args.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel );
                    if ( ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.MessagesBulkDeleted )
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                        {
                            Title = "**Messages Purged!**\n\n\n",
                            Color = DiscordColor.Red,
                            Description =
                            $"{args.Messages.Count} Messages were deleted in {args.Channel.Mention}.\n\n Attaching purge archive above:",
                            Timestamp = DateTime.Now,
                        };

                        StringBuilder sb = new StringBuilder();

                        Helpers.ArchiveInput( CreateCommandContext( args.Guild.Id, args.Channel.Id ), args.Messages, args.Channel );

                        using FileStream fs = new FileStream( $"{AppDomain.CurrentDomain.BaseDirectory}Export.zip", FileMode.Open, FileAccess.Read );

                        DiscordMessage msg = await new DiscordMessageBuilder()
                            .AddFile( $"{AppDomain.CurrentDomain.BaseDirectory}Export.zip", fs )
                            .AddEmbed( embed )
                            .SendAsync( channel );
                    }
                    else
                    {
                        return;
                    }
                }
            }
            return;
        }

        public CommandContext CreateCommandContext( ulong guildId, ulong channelId )
        {
            CommandsNextExtension cmds = this.Client.GetCommandsNext();
            Command cmd = cmds.FindCommand( "isolate", out string? customArgs );
            customArgs = "[]help. Hunting For Pulsars.";
            DiscordGuild guild = Program.Core.Client.GetGuildAsync( guildId ).Result;

            DiscordChannel channel = guild.GetChannel( channelId ) ?? throw new Exception( "Invalid channel id." );
            CommandContext context = cmds.CreateFakeContext( Core.Client.CurrentUser, channel, "isolate", ">", cmd, customArgs );
            return context;
        }

        private async Task Event_MessageCreated( DiscordClient sender, MessageCreateEventArgs e )
        {
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( ServerProfiles[i].LogConfig.ExcludedChannels.Contains( e.Channel.Id ) )
                    return;
            }

            if ( e.Message.Content.Contains( Core.Client.CurrentUser.Mention ) )
            {
                CommandContext context = CreateCommandContext( e.Guild.Id, e.Channel.Id );
                await context.RespondAsync( $"War is peace. Freedom is slavery. Ignorance is strength." );
            }

            if ( e.Author.Id == Core.Client.CurrentUser.Id || e.Author.Id == OwnerId )
            {
                return;
            }

            DiscordMember user = null;

            try
            {
                user = e.Guild.GetMemberAsync( e.Message.Author.Id ).Result;
            }
            catch ( Exception )
            {
                return;
            }

            Permissions perms = user.Permissions;

            if ( perms.HasPermission( Permissions.Administrator ) ||
                 perms.HasPermission( Permissions.BanMembers ) ||
                 perms.HasPermission( Permissions.KickMembers ) ||
                 perms.HasPermission( Permissions.ManageChannels ) ||
                 perms.HasPermission( Permissions.ManageGuild ) ||
                 perms.HasPermission( Permissions.ManageMessages ) ||
                 perms.HasPermission( Permissions.ManageRoles ) ||
                 perms.HasPermission( Permissions.ManageEmojis ) )
            {
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( e.Guild.Id );

            if ( profile == null ) return;

            foreach ( string word in profile.WordBlackList )
            {
                if ( e.Message.Content.Contains( word ) )
                {
                    CommandContext context = CreateCommandContext( e.Guild.Id, profile.LogConfig.MajorNotificationsChannelId );

                    await context.RespondAsync( $"The following user {e.Author.Mention} mentioned \"{word}\" in {e.Channel.Mention}." );
                    await context.RespondAsync( "Message Jump Link: " + e.Message.JumpLink.ToString() );
                    break;
                }
            }

            foreach ( string link in Database.ScamLinks )
            {
                if ( e.Message.Content.Contains( link ) )
                {
                    CommandContext fakeContext = CreateCommandContext( e.Guild.Id, e.Channel.Id );

                    await user.GrantRoleAsync( e.Guild.GetRole( profile.LogConfig.DefaultContainmentRoleId ) );
                    await fakeContext.RespondAsync(
                        $"Isolated user {user.Mention} at {e.Guild.GetChannel( profile.LogConfig.DefaultContainmentChannelId ).Mention}. " +
                        $"The user's message contained a discord scam link, the link was: `{link}`. " +
                        $"Revoked the following roles from the user: {string.Join( ", ", user.Roles.Select( x => x.Mention ).ToArray() )}."
                    );

                    foreach ( DiscordRole role in user.Roles )
                    {
                        await user.RevokeRoleAsync( role );
                    }

                    UserProfile userP = JsonConvert.DeserializeObject<UserProfile>(
                          File.ReadAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{e.Guild.Id}UserProfiles\{e.Author.Id}.json" ) );

                    userP.PunishmentEntries.Add( (DateTime.Now, "User posted a scam message.") );

                    File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{e.Guild.Id}UserProfiles\{e.Author.Id}.json",
                         JsonConvert.SerializeObject( userP, Formatting.Indented ) );

                    await e.Message.DeleteAsync();
                }
            }

            if ( !profile.AntiSpamModuleActive ) return;

            if ( !profile.AntiSpamIgnored.Contains( e.Channel.Id ) )
            {
                if ( _temporaryMessageCounter.Count <= 0 )
                {
                    _temporaryMessageCounter.Add( (e.Author.Id, 1) );
                }

                bool found = false;
                for ( int i = 0; i < _temporaryMessageCounter.Count; i++ )
                {
                    if ( _temporaryMessageCounter[i].Item1 == e.Author.Id )
                    {
                        found = true;
                        break;
                    }
                }

                if ( !found )
                {
                    _temporaryMessageCounter.Add( (e.Author.Id, 1) );
                }

                for ( int i = 0; i < _temporaryMessageCounter.Count; i++ )
                {
                    if ( _temporaryMessageCounter[i].Item1 == e.Author.Id )
                    {
                        _temporaryMessageCounter[i] = (e.Author.Id, _temporaryMessageCounter[i].Item2 + 1);
                        if ( _temporaryMessageCounter[i].Item2 >= profile.AntiSpamProfile.FirstWarning )
                        {
                            CommandContext fakeContext = CreateCommandContext( e.Guild.Id, e.Channel.Id );

                            if ( _temporaryMessageCounter[i].Item2 == profile.AntiSpamProfile.FirstWarning )
                            {
                                await fakeContext.RespondAsync( $"{e.Author.Mention} Stop sending messages so quickly." );
                            }
                            else if ( _temporaryMessageCounter[i].Item2 == profile.AntiSpamProfile.SecondWarning )
                            {
                                await fakeContext.RespondAsync( $"{e.Author.Mention} Your actions are considered spam." );
                            }
                            else if ( _temporaryMessageCounter[i].Item2 == profile.AntiSpamProfile.LastWarning )
                            {
                                await fakeContext.RespondAsync( $"{e.Author.Mention} This is your final warning, calm down." );
                            }
                            else if ( _temporaryMessageCounter[i].Item2 > profile.AntiSpamProfile.Limit )
                            {
                                await fakeContext.RespondAsync( $"{e.Author.Mention} You will be isolated now." );
                                IReadOnlyList<DiscordMessage> messages = await fakeContext.Channel.GetMessagesAsync( _temporaryMessageCounter[i].Item2 + 4 );
                                await fakeContext.Channel.DeleteMessagesAsync( messages );
                                await user.GrantRoleAsync( e.Guild.GetRole( profile.LogConfig.DefaultContainmentRoleId ) );
                                await fakeContext.RespondAsync(
                                    $"Isolated user {user.Mention} at {e.Guild.GetChannel( profile.LogConfig.DefaultContainmentChannelId ).Mention}. " +
                                    $"The user's actions were considered spam. " +
                                    $"Revoked the following roles from the user: {string.Join( ", ", user.Roles.Select( x => x.Mention ).ToArray() )}."
                                );
                                foreach ( var role in user.Roles )
                                {
                                    await user.RevokeRoleAsync( role );
                                }

                                var userP = JsonConvert.DeserializeObject<UserProfile>(
                                     File.ReadAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{e.Guild.Id}UserProfiles\{e.Author.Id}.json" ) );

                                userP.PunishmentEntries.Add( (DateTime.Now, "User's actions were considered spam.") );

                                File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{e.Guild.Id}UserProfiles\{e.Author.Id}.json",
                                     JsonConvert.SerializeObject( userP, Formatting.Indented ) );
                            }
                        }
                        break;
                    }
                }
            }
            return;
        }

        private async Task Event_InviteDeleted( DiscordClient sender, InviteDeleteEventArgs e )
        {
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( e.Guild.Id == ServerProfiles[i].ID )
                {
                    if ( ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.InviteDeleted )
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
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

        private async Task Event_InviteCreated( DiscordClient sender, InviteCreateEventArgs e )
        {
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( e.Guild.Id == ServerProfiles[i].ID )
                {
                    if ( ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.InviteCreated )
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
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

        private async Task Event_ChannelUpdated( DiscordClient sender, ChannelUpdateEventArgs e )
        {
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( e.Guild.Id == ServerProfiles[i].ID )
                {
                    if ( ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.ChannelUpdated )
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
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
                            "```\nThe Channel's ID is: " + e.ChannelAfter.Id + "\n" +
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

        private async Task Event_ChannelDeleted( DiscordClient sender, ChannelDeleteEventArgs e )
        {
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( e.Guild.Id == ServerProfiles[i].ID )
                {
                    if ( ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.ChannelDeleted )
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                        {
                            Title = "**Channel Deleted**",
                            Color = DiscordColor.Red,
                            Description =
                            $"**The channel's name was** {e.Channel.Name}\n\n" +
                            $"**Was the channel marked as nsfw?** {e.Channel.IsNSFW}\n\n" +
                            $"**The channel's type was** {e.Channel.Type}\n\n" +
                            $"**The channel's topic was** {e.Channel.Topic}\n\n" +
                            $"**The channel's category was** {e.Channel.Parent.Name}\n\n" +
                            "```\nThe Channel's ID was: " + e.Channel.Id + "\n" +
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

        private async Task Event_ChannelCreated( DiscordClient sender, ChannelCreateEventArgs e )
        {
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( e.Guild.Id == ServerProfiles[i].ID )
                {
                    if ( ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.ChannelCreated )
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                        {
                            Title = "**Channel Created**",
                            Color = DiscordColor.Wheat,
                            Description =
                            $"**The channel's name is** {e.Channel.Name}\n\n" +
                            $"**Is the channel marked as nsfw?** {e.Channel.IsNSFW}\n\n" +
                            $"**The channel's type is** {e.Channel.Type}\n\n" +
                            $"**The channel's topic is** {e.Channel.Topic}\n\n" +
                            $"**The channel's category is** {e.Channel.Parent.Name}\n\n" +
                            "```\nThe Channel's ID is: " + e.Channel.Id + "\n" +
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

        private async Task Event_MessageUpdated( DiscordClient sender, MessageUpdateEventArgs e )
        {
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( ServerProfiles[i].LogConfig.ExcludedChannels.Contains( e.Channel.Id ) )
                    return;
            }
            if ( e.Message.Timestamp < Core.BotStartUpStamp )
            {
                return;
            }
            if ( e.Message.Author.IsBot )
            {
                return;
            }

            ServerProfile profile = ServerProfile.ProfileFromId( e.Guild.Id );

            if ( profile == null ) return;

            foreach ( string word in profile.WordBlackList )
            {

                DiscordMember user = e.Guild.GetMemberAsync( e.Message.Author.Id ).Result;

                if ( e.Message.Content.Contains( word ) )
                {
                    CommandsNextExtension cmds = Program.Core.Client.GetCommandsNext();
                    Command cmd = cmds.FindCommand( "isolate", out var customArgs );
                    customArgs = "[]help. Hunting For Pulsars.";
                    DiscordGuild guild = Program.Core.Client.GetGuildAsync( e.Guild.Id ).Result;
                    await cmds.CreateFakeContext( user, guild.GetChannel( profile.LogConfig.MajorNotificationsChannelId ), "isolate", ">", cmd, customArgs )
                        .RespondAsync(
                        $"The following user {e.Author.Mention} mentioned {word} in {e.Channel.Mention}, message link: {e.Message.JumpLink}.\n" +
                        $"The user mentioned the black-listed word after editing a message."
                    );
                    break; ;
                }
            }

            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( e.Guild.Id == ServerProfiles[i].ID )
                {
                    if ( ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.MessageUpdated )
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
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
                            "```\nThe user's ID is: " + e.Message.Author.Id + "\n" +
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

        private async Task Event_MessageReactionAdded( DiscordClient sender, MessageReactionAddEventArgs e )
        {
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( ServerProfiles[i].LogConfig.ExcludedChannels.Contains( e.Channel.Id ) )
                    return;
            }
            if ( e.Message.Timestamp < Core.BotStartUpStamp )
            {
                return;
            }
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( e.Guild.Id == ServerProfiles[i].ID )
                {
                    if ( ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.MessageReactionAdded )
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
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
                            "```\nThe user's ID is: " + e.Message.Author.Id + "\n" +
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

        private async Task Event_MessageReactionsCleared( DiscordClient sender, MessageReactionsClearEventArgs e )
        {
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( ServerProfiles[i].LogConfig.ExcludedChannels.Contains( e.Channel.Id ) )
                    return;
            }
            if ( e.Message.Timestamp < Core.BotStartUpStamp )
            {
                return;
            }
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( e.Guild.Id == ServerProfiles[i].ID )
                {
                    if ( ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.MessageReactionsCleared )
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
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

        private async Task Event_MessageReactionRemoved( DiscordClient sender, MessageReactionRemoveEventArgs e )
        {
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( ServerProfiles[i].LogConfig.ExcludedChannels.Contains( e.Channel.Id ) )
                    return;
            }
            if ( e.Message.Timestamp < Core.BotStartUpStamp )
            {
                return;
            }
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( e.Guild.Id == ServerProfiles[i].ID )
                {
                    if ( ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.MessageReactionRemoved )
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
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
                            "```\nThe user's ID is: " + e.Message.Author.Id + "\n" +
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

        private async Task Event_GuildRoleDeleted( DiscordClient sender, GuildRoleDeleteEventArgs e )
        {
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( e.Guild.Id == ServerProfiles[i].ID )
                {
                    if ( ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.GuildRoleDeleted )
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                        {
                            Title = "**Role Deleted**",
                            Color = DiscordColor.Red,
                            Description =
                            $"**The role's name was:** {e.Role.Name}\n\n" +
                            $"**The role's tags were:** {e.Role.Tags}\n\n" +
                            $"**Was the role mentionable?** {e.Role.IsMentionable}\n\n" +
                            $"**The role's color was:** {e.Role.Color}\n\n" +
                            $"```\nThe role's id was: {e.Role.Id}\n" +
                            $"The role was created at: {e.Role.CreationTimestamp.UtcDateTime}\n" +
                            $"The role was deleted at: {DateTime.Now}```",
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

        private async Task Event_GuildRoleUpdated( DiscordClient sender, GuildRoleUpdateEventArgs e )
        {
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( e.Guild.Id == ServerProfiles[i].ID )
                {
                    if ( ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.GuildRoleUpdated )
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                        {
                            Title = "**Role Updated**",
                            Color = DiscordColor.Wheat,
                            Description =
                            $"**The role' name change from-to:** {e.RoleBefore.Name} -> {e.RoleAfter.Name}\n\n" +
                            $"**The role's tags changed from-to:** {e.RoleBefore.Tags} -> {e.RoleAfter.Tags}\n\n" +
                            $"**Is the role mentionable?** {e.RoleAfter.IsMentionable}\n\n" +
                            $"**The role's color changed from-to:** {e.RoleBefore.Color} -> {e.RoleAfter.Color}\n\n" +
                            $"```\nThe role's id is: {e.RoleAfter.Id}\n" +
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

        private async Task Event_GuildRoleCreated( DiscordClient sender, GuildRoleCreateEventArgs e )
        {
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( e.Guild.Id == ServerProfiles[i].ID )
                {
                    if ( ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.GuildRoleCreated )
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                        {
                            Title = "**Role Created**",
                            Color = DiscordColor.Wheat,
                            Description =
                            $"**The role's name is:** {e.Role.Name}\n\n" +
                            $"**The role's tags are:** {e.Role.Tags}\n\n" +
                            $"**Is the role mentionable?** {e.Role.IsMentionable}\n\n" +
                            $"**The role's color is:** {e.Role.Color}\n\n" +
                            $"```\nThe role's id is: {e.Role.Id}\n" +
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

        private async Task Event_GuildBanAdded( DiscordClient sender, GuildBanAddEventArgs e )
        {
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( e.Guild.Id == ServerProfiles[i].ID )
                {
                    if ( ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.GuildBanAdded )
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
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
                            "```\nThe user's ID is: " + e.Member.Id + "```",
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

        private async Task Event_GuildBanRemoved( DiscordClient sender, GuildBanRemoveEventArgs e )
        {
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( e.Guild.Id == ServerProfiles[i].ID )
                {
                    if ( ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.GuildBanAdded )
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
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
                            "```\nThe user's ID is: " + e.Member.Id + "```",
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

        private async Task Event_GuildMemberAdded( DiscordClient sender, GuildMemberAddEventArgs e )
        {
            ServerProfile serverProfile = ServerProfile.ProfileFromId( e.Guild.Id );
            if ( serverProfile.HasCustomWelcome )
            {
                if ( serverProfile.CustomWelcome.RoleId != 0 )
                {
                    await e.Member.GrantRoleAsync( e.Guild.GetRole( serverProfile.CustomWelcome.RoleId ) );
                }
                DiscordChannel main = e.Guild.GetChannel( serverProfile.CustomWelcome.ChannelId );
                await main.SendMessageAsync( serverProfile.CustomWelcome.Message.Replace( "MENTION", $"{e.Member.Mention}" ) );
            }
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( e.Guild.Id == ServerProfiles[i].ID )
                {
                    if ( !File.Exists( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{e.Guild.Id}UserProfiles\{e.Member.Id}.json" ) )
                    {
                        UserProfile profile = new UserProfile( e.Member.Id )
                        {
                            Discriminator = e.Member.Discriminator,
                            CreationDate = e.Member.CreationTimestamp,
                            FirstJoinDate = e.Member.JoinedAt,
                            LocalLanguage = e.Member.Locale
                        };

                        profile.LastJoinDate = DateTime.Now;

                        File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{e.Guild.Id}UserProfiles\{e.Member.Id}.json",
                           JsonConvert.SerializeObject( profile, Formatting.Indented ) );
                    }

                    if ( ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.GuildMemberAdded )
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
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
                            "```\nThe user's ID is: " + e.Member.Id + "```",
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

        private async Task Event_GuildMemberRemoved( DiscordClient sender, GuildMemberRemoveEventArgs e )
        {
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( e.Guild.Id == ServerProfiles[i].ID )
                {
                    UserProfile user = JsonConvert.DeserializeObject<UserProfile>(
                        File.ReadAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{e.Guild.Id}UserProfiles\{e.Member.Id}.json" ) );

                    user.LeaveDate = DateTime.Now;

                    File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{e.Guild.Id}UserProfiles\{e.Member.Id}.json",
                       JsonConvert.SerializeObject( user, Formatting.Indented ) );

                    if ( ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.GuildMemberRemoved )
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
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
                            "```\nThe user's ID is: " + e.Member.Id + "\n```",
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

        private async Task Event_MessageDeleted( DiscordClient sender, MessageDeleteEventArgs e )
        {
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( ServerProfiles[i].LogConfig.ExcludedChannels.Contains( e.Channel.Id ) )
                    return;
            }
            if ( e.Message.Timestamp < Core.BotStartUpStamp )
            {
                return;
            }
            for ( int i = 0; i < ServerProfiles.Count; i++ )
            {
                if ( e.Guild.Id == ServerProfiles[i].ID )
                {
                    if ( ServerProfiles[i].LogConfig.LoggingEnabled && ServerProfiles[i].LogConfig.MessageDeleted )
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                        {
                            Title = "**Message Deleted**",
                            Color = DiscordColor.Red,
                            Author = new DiscordEmbedBuilder.EmbedAuthor
                            {
                                Name = e.Message.Author.Username + "#" + e.Message.Author.Discriminator,
                                IconUrl = e.Message.Author.AvatarUrl
                            },
                            Description =
                            $"\n\n**The deleted message was:** " + $"{(e.Message.Content == "" ? "None, see attachment(s) below." : e.Message.Content)} " + "\n" +
                            $"\n **Message deleted at:** {e.Channel.Mention} \n\n" +
                            "```\nThe user's ID is: " + e.Message.Author.Id + "\n" +
                            "The deleted message's ID was: " + e.Message.Id + "\n" +
                            "The Channel's ID is: " + e.Channel.Id + "```",
                            Timestamp = DateTime.Now,
                        };

                        int z = 0;
                        if ( e.Message.Attachments != null )
                        {
                            foreach ( var item in e.Message.Attachments )
                            {
                                await Console.Out.WriteLineAsync(item.Url);
                                string savePath = $"{AppDomain.CurrentDomain.BaseDirectory}\\Temp\\image{z}.{GetUrlType(item.Url)}";

                                try
                                {
                                    using ( HttpClient client = new HttpClient() )
                                    {
                                        byte[] imageBytes = await client.GetByteArrayAsync( item.Url );
                                        await File.WriteAllBytesAsync( savePath, imageBytes );
                                        Console.WriteLine( "Image downloaded successfully." );
                                    }
                                }
                                catch ( Exception ex )
                                {
                                    Console.WriteLine( $"Error downloading image: {ex.Message}" );
                                }
                                z++;
                            }
                        }

                        var channel = e.Guild.GetChannel( ServerProfiles[i].LogConfig.LogChannel );
                        await channel.SendMessageAsync( embed );

                        var saved = Helpers.GetAllFilesFromFolder( @$"{AppDomain.CurrentDomain.BaseDirectory}\Temp\", false );

                        foreach ( var item in saved )
                        {
                            using var fs = new FileStream( item, FileMode.Open, FileAccess.Read );
                            var msg = await new DiscordMessageBuilder().AddFile( item, fs ).SendAsync( channel );
                        }
                        foreach ( var item in saved )
                        {
                            File.Delete( item );
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

        private string GetUrlType ( string url )
        {
            if ( url.Contains( ".jpg" ) || url.Contains( ".jpeg" ) ) return ".jpg";
            if ( url.Contains( ".mp4" ) ) return ".mp4";
            if ( url.Contains( ".mp3" ) ) return ".mp3";
            if ( url.Contains( ".png" ) ) return ".png";
            if ( url.Contains( ".gif" ) ) return ".gif";

            return ".jpg";
        }

        private Task Client_Ready( DiscordClient sender, ReadyEventArgs e )
        {
            Core.Client.UpdateStatusAsync( new DiscordActivity( "You", ActivityType.Watching ), UserStatus.DoNotDisturb, DateTimeOffset.Now );
            sender.Logger.LogInformation( BotEventId, "Client is ready to process events." );
            return Task.CompletedTask;
        }

        private Task Client_GuildAvailable( DiscordClient sender, GuildCreateEventArgs e )
        {
            sender.Logger.LogInformation( BotEventId, $"Guild available: {e.Guild.Name}" );
            return Task.CompletedTask;
        }

        private Task Client_ClientError( DiscordClient sender, ClientErrorEventArgs e )
        {
            sender.Logger.LogError( BotEventId, e.Exception, "Exception occured" );
            return Task.CompletedTask;
        }
    }
}
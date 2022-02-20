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

        private string _token;
        private Timer _entryCheckTimer;
        private readonly EventId BotEventId = new( 69, "Bot-Ex14" );

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

        public async Task RunBotAsync ()
        {
            Config Info = JsonConvert.DeserializeObject<Config>( File.ReadAllText( AppDomain.CurrentDomain.BaseDirectory + @"Config.json" ));

            Core.SetToken( Info.Token );
            Core.OwnerId = Info.OwnerId;

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

            Commands.RegisterCommands<GeneralCommands>();
            Commands.RegisterCommands<Help>();

            Core.Client = this.Client;
            await this.Client.ConnectAsync();
            await Task.Delay( -1 );
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
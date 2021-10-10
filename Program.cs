using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using GreyCrammedContainer;
using System.Reflection;

namespace Icarus
{
    class Program
    {
        public static string Token;
        public static ulong OwnerID;
        
        public DiscordClient Client { get; private set; }
        public CommandsNextConfiguration CommandsNextConfig { get; private set; }
        public CommandsNextExtension Commands { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }
        public readonly EventId BotEventId = new( 42, "Bot-Ex03" );

        static void Main ( string[] args )
        {
            Program App = new Program();

            if (GetOperatingSystem() == OSPlatform.Windows)
            {
                Console.WindowWidth = 140;
                Console.WindowHeight = 30;
            }

            App.RunBotAsync().GetAwaiter().GetResult();
            Console.Clear();
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
            Program.Token = Info.Token;
            Program.OwnerID = Info.OwnerID;
            var cfg = new DiscordConfiguration
            {
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
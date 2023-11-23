using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Text;
using System.IO.Compression;
using System.Net;

using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus;
using DSharpPlus.SlashCommands.Attributes;

namespace Icarus.Modules.Other
{
    public class SlashCommands : ApplicationCommandModule
    {
        [SlashCommand( "GiveIn", "Willfully report your personal information to GreySoc." )]
        public async Task TestCommand( InteractionContext ctx )
        {
            await ctx.CreateResponseAsync( InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent( "Your home address, coordinates and credit card information has been added to the GreySoc database, we thank you for your obedience." ) );
        }

        [SlashCommand( "ping", "Command for bot latency time." )]
        [Description( "Responds with ping time." )]
        public async Task Ping( InteractionContext ctx )
        {
            await ctx.CreateResponseAsync( $"Ping: {ctx.Client.Ping}ms." );
        }
        
        [SlashCommand( "erase", $"Deletes N amount of messages if possible." )]
        [Description( "Deletes set amount of messages if possible." )]
        [SlashRequirePermissions( DSharpPlus.Permissions.ManageMessages )]
        public async Task Erase( InteractionContext ctx, [Option( "Count", "The amount of messages to delete." )] long N )
        {
            try
            {
                var messages = await ctx.Channel.GetMessagesAsync( Convert.ToInt32( N ) );
                await ctx.Channel.DeleteMessagesAsync( messages );
                await ctx.CreateResponseAsync( $"Erased: {N} messages, called by {ctx.User.Mention}." );
            }
            catch ( Exception )
            {
                await ctx.CreateResponseAsync( "Failed to erase, are the messages too old?" );
            }
        }
        
        [SlashCommand( "eraseAggressive", "Deletes N amount of messages, can delete messages older than 2 weeeks." )]
        [Description( "Deletes set amount of messages if possible, can delete messages older than 2 weeeks." )]
        [SlashRequirePermissions( DSharpPlus.Permissions.ManageMessages )]
        public async Task EraseAggressive( InteractionContext ctx, [Option( "Count", "The amount of messages to delete." )] long N )
        {
            try
            {
                var messages = await ctx.Channel.GetMessagesAsync( (int)N );
                foreach ( var item in messages )
                {
                    await ctx.Channel.DeleteMessageAsync( item );
                }
                await ctx.CreateResponseAsync( $"Erased: {N} messages, called by {ctx.User.Mention}." );
            }
            catch ( Exception )
            {
                await ctx.CreateResponseAsync( "Failed to erase." );
            }
        }
        
        [SlashCommand( "archive", "Exports a discord channel and sends it as a .zip file." )]
        [Description( "Exports a discord channel and sends it as a .zip file." )]
        [SlashRequirePermissions( DSharpPlus.Permissions.ManageMessages )]
        public async Task Archive( InteractionContext ctx, [Option( "Channel", "The channel to export." )] DiscordChannel ch )
        {
            if ( !ctx.Guild.Channels.ContainsKey( ch.Id ) )
            {
                await ctx.CreateResponseAsync( $"Invalid channel Id: {ch.Id}" );
                return;
            }

            string exportPath = $"{AppDomain.CurrentDomain.BaseDirectory}Export.zip";

            if ( File.Exists( exportPath ) )
            {
                File.Delete( exportPath );
            }

            try
            {
                StringBuilder sb = new( Constants.ChannelExportFirstHalf.Length );
                DiscordChannel channel = ctx.Guild.GetChannel( ch.Id );

                sb.Append( @$"<!DOCTYPE html><html lang=""en""><head><title>{ctx.Guild.Name} {channel.Name}</title>" );
                sb.Append( Constants.ChannelExportFirstHalf );

                var exportsDir = $"Exports\\";
                var exportId = $"{ctx.Guild.Name.Replace( " ", "" )}{new Random().Next( 1, int.MaxValue )}";
                var fileDir = $"{exportsDir}\\{exportId}.html";
                var imagesDir = $"{exportsDir}{exportId}_Images\\";

                using var client = new WebClient();
                var iconPath = $"{imagesDir}\\{ctx.Guild.Name}.jpg";

                sb.Append( @"<body><div class=""preamble""><div class=""preamble__guild-icon-container""><img class=""preamble__guild-icon"" src=""" );
                sb.Append( $"{exportId}_Images\\{ctx.Guild.Name}.jpg" );
                sb.Append( @""" alt=""Guild icon"" loading=""lazy""></div><div class=""preamble__entries-container""><div class=""preamble__entry"">" );
                sb.Append( ctx.Guild.Name );
                sb.Append( @"</div><div class=""preamble__entry"">" );
                sb.Append( $"{channel.Parent.Name.ToLowerInvariant()} / {channel.Name}" );
                sb.Append( @"</div><div class=""preamble__entry preamble__entry--small"">" );
                sb.Append( channel.Topic );
                sb.Append( @"</div></div></div><div class=""chatlog"">" );

                var messages = ch.GetMessagesAsync( 100000000 ).Result.ToArray();
                ulong oldId = 0;
                bool open = false;

                foreach ( DiscordMessage item in messages.Reverse() )
                {
                    if ( item.Author == null ) continue;

                    // new message, new author
                    if ( oldId != item.Author.Id && !open )
                    {
                        oldId = item.Author.Id;
                        open = true;
                        sb.Append( $@"        
        <div class=""chatlog__message-group"">
            <div id=""chatlog__message-container-{item.Id}"" class=""chatlog__message-container "" data-message-id=""{item.Id}"">
                <div class=""chatlog__message"">
                    <div class=""chatlog__message-aside"">" );
                        sb.Append( $@"
                        <img class=""chatlog__avatar"" src=""{exportId}_Images\{item.Author.Username}.jpg"" alt=""Avatar"" loading=""lazy"">
                    </div>
                    <div class=""chatlog__message-primary"">
                        <div class=""chatlog__header"">
                            <span class=""chatlog__author"" style="""" title=""{item.Author.Username}"" data-user-id=""{item.Author.Id}"">{item.Author.Username}</span>
                            <span class=""chatlog__timestamp"">{item.Timestamp}</span>
                        </div>" );
                        if ( item.ReferencedMessage != null )
                        {
                            sb.Append( $@"
                        <div class=""chatlog__reference"">
                            <img class=""chatlog__reference-avatar"" src=""{exportId}_Images\{item.ReferencedMessage.Author.Username}.jpg"" alt=""Avatar"" loading=""lazy"">
                            <div class=""chatlog__reference-author"" title=""{item.ReferencedMessage.Author.Username}"">{item.ReferencedMessage.Author.Username}</div>
                            <div class=""chatlog__reference-content"">
                                <span class=""chatlog__reference-link"" onclick=""scrollToMessage(event, &#39;{item.ReferencedMessage.Id}&#39;)"">
                                    {item.ReferencedMessage.Content}
                                </span>
                            </div>
                        </div>" );
                        }
                        if ( item.MentionedUsers.Count > 0 )
                        {
                            sb.Append( $@"
                        <div class=""chatlog__content chatlog__markdown"">
                            <span class=""chatlog__markdown-preserve""><span class=""chatlog__markdown-mention"" title=""{item.MentionedUsers[0].Username}"">{string.Join( ' ', item.MentionedUsers.Select( x => x.Username ) )}</span>{item.Content}</span>
                        </div>" );
                        }
                        else
                        {
                            sb.Append( $@"
                        <div class=""chatlog__content chatlog__markdown"">
                            <span class=""chatlog__markdown-preserve"">{item.Content}</span>
                        </div>" );
                        }
                        if ( item.Attachments.Count > 0 )
                        {
                            foreach ( var att in item.Attachments )
                            {
                                sb.Append( $@"
                        <div class=""chatlog__attachment "" onclick="""">
                                <img class=""chatlog__attachment-media"" src=""{exportId}_Images\{att.FileName}"" alt=""Image attachment"" title=""{att.FileSize}"" loading=""lazy"">
                        </div>" );
                            }
                        }
                        sb.Append( $@"
                    </div>
                </div>
            </div>" );
                    }
                    // new message, new author, old author message needs to be closed
                    else if ( oldId != item.Author.Id && open )
                    {
                        oldId = item.Author.Id;
                        sb.Append( "</div>" );
                        sb.Append( $@"        
        <div class=""chatlog__message-group"">
            <div id=""chatlog__message-container-{item.Id}"" class=""chatlog__message-container "" data-message-id=""{item.Id}"">
                <div class=""chatlog__message"">
                    <div class=""chatlog__message-aside"">" );
                        if ( item.ReferencedMessage != null )
                        {
                            sb.Append( "<div class=\"chatlog__reference-symbol\"></div>" );
                        }
                        sb.Append( $@"
                        <img class=""chatlog__avatar"" src=""{exportId}_Images\{item.Author.Username}.jpg"" alt=""Avatar"" loading=""lazy"">
                    </div>
                    <div class=""chatlog__message-primary"">" );
                        if ( item.ReferencedMessage != null )
                        {
                            sb.Append( $@"
                        <div class=""chatlog__reference"">
                            <img class=""chatlog__reference-avatar"" src=""{exportId}_Images\{item.ReferencedMessage.Author.Username}.jpg"" alt=""Avatar"" loading=""lazy"">
                            <div class=""chatlog__reference-author"" title=""{item.ReferencedMessage.Author.Username}"">{item.ReferencedMessage.Author.Username}</div>
                            <div class=""chatlog__reference-content"">
                                <span class=""chatlog__reference-link"" onclick=""scrollToMessage(event, &#39;{item.ReferencedMessage.Id}&#39;)"">
                                    {item.ReferencedMessage.Content}
                                </span>
                            </div>
                        </div>" );
                        }
                        sb.Append( $@"
                        <div class=""chatlog__header"">
                            <span class=""chatlog__author"" style="""" title=""{item.Author.Username}"" data-user-id=""{item.Author.Id}"">{item.Author.Username}</span>
                            <span class=""chatlog__timestamp"">{item.Timestamp}</span>
                        </div>" );
                        if ( item.MentionedUsers.Count > 0 )
                        {
                            sb.Append( $@"
                        <div class=""chatlog__content chatlog__markdown"">
                            <span class=""chatlog__markdown-preserve""><span class=""chatlog__markdown-mention"" title=""{item.MentionedUsers[0].Username}"">{string.Join( ' ', item.MentionedUsers.Select( x => x.Username ) )}</span>{item.Content}</span>
                        </div>" );
                        }
                        else
                        {
                            sb.Append( $@"
                        <div class=""chatlog__content chatlog__markdown"">
                            <span class=""chatlog__markdown-preserve"">{item.Content}</span>
                        </div>" );
                        }
                        if ( item.Attachments.Count > 0 )
                        {
                            foreach ( var att in item.Attachments )
                            {
                                sb.Append( $@"
                        <div class=""chatlog__attachment "" onclick="""">
                                <img class=""chatlog__attachment-media"" src=""{exportId}_Images\{att.FileName}"" alt=""Image attachment"" title=""{att.FileSize}"" loading=""lazy"">
                        </div>" );
                            }
                        }
                        sb.Append( $@"
                    </div>
                </div>
            </div>" );
                    }
                    // new message, same author
                    else if ( oldId == item.Author.Id && open )
                    {
                        sb.Append( $@"
            <div id=""chatlog__message-container-{item.Id}"" class=""chatlog__message-container "" data-message-id=""{item.Id}"">
                <div class=""chatlog__message"">
                    <div class=""chatlog__message-aside"">
                        <div class=""chatlog__short-timestamp"" title=""{item.Timestamp}""></div>
                    </div>" );
                        if ( item.ReferencedMessage != null )
                        {
                            sb.Append( $@"
                    <div class=""chatlog__message-aside"">
                        <div class=""chatlog__reference-symbol""></div>
                        <img class=""chatlog__avatar"" src=""{exportId}_Images\{item.Author.Username}.jpg"" alt=""Avatar"" loading=""lazy"">
                    </div>" );
                        }
                        sb.Append( @"<div class=""chatlog__message-primary"">" );
                        if ( item.ReferencedMessage != null )
                        {
                            sb.Append( $@"
                        <div class=""chatlog__reference"">
                            <img class=""chatlog__reference-avatar"" src=""{exportId}_Images\{item.ReferencedMessage.Author.Username}.jpg"" alt=""Avatar"" loading=""lazy"">
                            <div class=""chatlog__reference-author"" title=""{item.ReferencedMessage.Author.Username}"">{item.ReferencedMessage.Author.Username}</div>
                            <div class=""chatlog__reference-content"">
                                <span class=""chatlog__reference-link"" onclick=""scrollToMessage(event, &#39;{item.ReferencedMessage.Id}&#39;)"">
                                    {item.ReferencedMessage.Content}
                                </span>
                            </div>
                        </div>" );
                        }
                        if ( item.MentionedUsers.Count > 0 )
                        {
                            sb.Append( $@"
                        <div class=""chatlog__content chatlog__markdown"">
                            <span class=""chatlog__markdown-preserve""><span class=""chatlog__markdown-mention"" title=""{item.MentionedUsers[0].Username}"">{string.Join( ' ', item.MentionedUsers.Select( x => x.Username ) )}</span>{item.Content}</span>
                        </div>" );
                        }
                        else
                        {
                            sb.Append( $@"
                        <div class=""chatlog__content chatlog__markdown"">
                            <span class=""chatlog__markdown-preserve"">{item.Content}</span>
                        </div>" );
                        }
                        if ( item.Attachments.Count > 0 )
                        {
                            foreach ( var att in item.Attachments )
                            {
                                sb.Append( $@"
                        <div class=""chatlog__attachment "" onclick="""">
                                <img class=""chatlog__attachment-media"" src=""{exportId}_Images\{att.FileName}"" alt=""Image attachment"" title=""{att.FileSize}"" loading=""lazy"">
                        </div>" );
                            }
                        }
                        sb.Append( $@"
                    </div>
                </div>
            </div>" );
                    }
                }

                sb.Append( $@"</div><div class=""postamble""><div class=""postamble__entry"">Exported {messages.Length} messages(s).</div></div>" );
                sb.Append( "</body></html>" );

                foreach ( var item in Directory.GetDirectories( "Exports\\" ) )
                {
                    if ( !item.EndsWith( "Data" ) )
                    {
                        foreach ( var file in Directory.GetFiles( item ) )
                        {
                            File.Delete( file );
                        }
                        Directory.Delete( item );
                    }
                }

                foreach ( var item in Directory.GetFiles( "Exports\\" ) )
                {
                    File.Delete( item );
                }

                Directory.CreateDirectory( imagesDir );

                client.DownloadFile( ctx.Guild.IconUrl, iconPath );
                File.WriteAllText( fileDir, sb.ToString() );

                foreach ( DiscordMessage item in messages )
                {
                    if ( item.Attachments.Count > 0 )
                    {
                        foreach ( DiscordAttachment att in item.Attachments )
                        {
                            var path = $"{imagesDir}\\{att.FileName}";
                            client.DownloadFile( att.Url, path );
                        }
                    }
                    string path2 = $"{imagesDir}{item.Author.Username}.jpg";
                    client.DownloadFile( item.Author.AvatarUrl, path2 );
                }

                ZipFile.CreateFromDirectory( @"Exports\\", exportPath );
                using var fs = new FileStream( exportPath, FileMode.Open, FileAccess.Read );

                var msg = await new DiscordMessageBuilder()
                    .AddFile( $"{channel.Name}.zip", fs )
                    .SendAsync( ctx.Channel );
            }
            catch ( Exception e )
            {
                Console.WriteLine( e );
                await ctx.CreateResponseAsync( "Failed to export channel, is the id provided valid?" );
            }
        }
    }
}
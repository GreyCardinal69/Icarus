using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Icarus.Modules.Profiles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Modules.Other
{
    public class GeneralCommands : BaseCommandModule
    {
        [Command( "BotTalk" )]
        [Description( "Command for talking as the bot." )]
        [RequireUserPermissions( DSharpPlus.Permissions.Administrator )]
        public async Task BotTalk( CommandContext ctx, ulong id, ulong id2, ulong id3, bool thread, params string[] rest )
        {
            await ctx.TriggerTypingAsync();
            CommandContext fakeContext = null;

            try
            {
                fakeContext = Program.Core.CreateCommandContext( id, id2 );
            }
            catch ( Exception ex )
            {
                await ctx.RespondAsync( ex.Message );
                throw;
            }

            if ( !thread )
            {
                await fakeContext.RespondAsync( string.Join( " ", rest ) );
            }
            else
            {
                DiscordChannel channel = fakeContext.Guild.GetChannel( id2 );

                if ( channel == null )
                {
                    await ctx.RespondAsync( "Invalid thread ID." );
                    return;
                }

                foreach ( DiscordThreadChannel item in channel.Threads )
                {
                    if ( item.Id == id3 )
                    {
                        await fakeContext.RespondAsync( string.Join( " ", rest ) );
                    }
                }
            }
        }

        [Command( "Ping" )]
        [Description( "Responds with ping time." )]
        public async Task Ping( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync( $"Ping: {ctx.Client.Ping}ms." );
        }

        [Command( "UpTime" )]
        [Description( "Responds with current instance up time." )]
        public async Task UpTime( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync( $"Uptime: {DateTime.Now - Program.Core.BotStartUpStamp}." );
        }

        [Command( "Erase" )]
        [Description( "Deletes set amount of messages if possible." )]
        [RequireUserPermissions( DSharpPlus.Permissions.ManageMessages )]
        public async Task Erase( CommandContext ctx, int count )
        {
            await ctx.TriggerTypingAsync();
            try
            {
                IReadOnlyList<DiscordMessage> messages = await ctx.Channel.GetMessagesAsync( count );
                await ctx.Channel.DeleteMessagesAsync( messages );
                await ctx.RespondAsync( $"Erased: {count} messages, called by {ctx.User.Mention}." );
            }
            catch ( Exception )
            {
                await ctx.RespondAsync( "Failed to erase, are the messages too old?" );
            }
        }

        [Command( "EraseAggressive" )]
        [Description( "Deletes set amount of messages if possible, can delete messages older than 2 weeeks." )]
        [RequireUserPermissions( DSharpPlus.Permissions.ManageMessages )]
        public async Task EraseAggressive( CommandContext ctx, int count )
        {
            await ctx.TriggerTypingAsync();
            try
            {
                IReadOnlyList<DiscordMessage> messages = await ctx.Channel.GetMessagesAsync( count );
                foreach ( DiscordMessage item in messages )
                {
                    await ctx.Channel.DeleteMessageAsync( item );
                }
                await ctx.RespondAsync( $"Erased: {count} messages, called by {ctx.User.Mention}." );
            }
            catch ( Exception )
            {
                await ctx.RespondAsync( "Failed to erase." );
            }
        }

        [Command( "Archive" )]
        [Description( "Exports a discord channel and sends it as a .zip file." )]
        [RequireUserPermissions( DSharpPlus.Permissions.ManageMessages )]
        public async Task Archive( CommandContext ctx, ulong id )
        {
            if ( !ctx.Guild.Channels.ContainsKey( id ) )
            {
                await ctx.RespondAsync( $"Invalid channel Id: {id}" );
                return;
            }

            string exportPath = $"{AppDomain.CurrentDomain.BaseDirectory}Export.zip";

            if ( File.Exists( exportPath ) )
            {
                File.Delete( exportPath );
            }

            await ctx.TriggerTypingAsync();
            try
            {
                StringBuilder sb = new StringBuilder( Constants.ChannelExportFirstHalf.Length );
                DiscordChannel channel = ctx.Guild.GetChannel( id );

                sb.Append( @$"<!DOCTYPE html><html lang=""en""><head><title>{ctx.Guild.Name} {channel.Name}</title>" );
                sb.Append( Constants.ChannelExportFirstHalf );

                string exportsDir = $"Exports\\";
                string exportId = $"{ctx.Guild.Name.Replace( " ", "" )}{new Random().Next( 1, int.MaxValue )}";
                string fileDir = $"{exportsDir}\\{exportId}.html";
                string imagesDir = $"{exportsDir}{exportId}_Images\\";

                using var client = new WebClient();
                string iconPath = $"{imagesDir}\\{ctx.Guild.Name}.jpg";

                sb.Append( @"<body><div class=""preamble""><div class=""preamble__guild-icon-container""><img class=""preamble__guild-icon"" src=""" );
                sb.Append( $"{exportId}_Images\\{ctx.Guild.Name}.jpg" );
                sb.Append( @""" alt=""Guild icon"" loading=""lazy""></div><div class=""preamble__entries-container""><div class=""preamble__entry"">" );
                sb.Append( ctx.Guild.Name );
                sb.Append( @"</div><div class=""preamble__entry"">" );
                sb.Append( $"{channel.Parent.Name.ToLowerInvariant()} / {channel.Name}" );
                sb.Append( @"</div><div class=""preamble__entry preamble__entry--small"">" );
                sb.Append( channel.Topic );
                sb.Append( @"</div></div></div><div class=""chatlog"">" );

                DiscordMessage[] messages = channel.GetMessagesAsync( 100000000 ).Result.ToArray();
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
                            foreach ( DiscordAttachment att in item.Attachments )
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
                            foreach ( DiscordAttachment att in item.Attachments )
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
                            foreach ( DiscordAttachment att in item.Attachments )
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

                foreach ( string item in Directory.GetDirectories( "Exports\\" ) )
                {
                    if ( !item.EndsWith( "Data" ) )
                    {
                        foreach ( string file in Directory.GetFiles( item ) )
                        {
                            File.Delete( file );
                        }
                        Directory.Delete( item );
                    }
                }

                foreach ( string item in Directory.GetFiles( "Exports\\" ) )
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
                    if ( !File.Exists( path2 ) )
                    {
                        client.DownloadFile( item.Author.AvatarUrl, path2 );
                    }
                }

                ZipFile.CreateFromDirectory( @"Exports\\", exportPath );
                using FileStream fs = new FileStream( exportPath, FileMode.Open, FileAccess.Read );

                DiscordMessage msg = await new DiscordMessageBuilder()
                    .AddFile( $"{channel.Name}.zip", fs )
                    .SendAsync( ctx.Channel );
            }
            catch ( Exception e )
            {
                Console.WriteLine( e );
                await ctx.RespondAsync( "Failed to export channel, is the id provided valid?" );
            }
        }

        [Command( "EraseFromTo" )]
        [Description( "Deletes all messages from the first to the second specified message." )]
        [RequireUserPermissions( DSharpPlus.Permissions.ManageMessages )]
        public async Task EraseFromTo( CommandContext ctx, ulong from, ulong to, int amount )
        {
            await ctx.TriggerTypingAsync();
            DiscordMessage fromMsg = await ctx.Channel.GetMessageAsync( from );
            DiscordMessage toMsg = await ctx.Channel.GetMessageAsync( to );

            IReadOnlyList<DiscordMessage> messagesBefore = await ctx.Channel.GetMessagesBeforeAsync( to, amount );
            IReadOnlyList<DiscordMessage> messagesAfter = await ctx.Channel.GetMessagesAfterAsync( from, amount );

            IEnumerable<DiscordMessage> filtered = messagesAfter.Union( messagesBefore ).Distinct().Where(
                x => ( DateTimeOffset.Now - x.Timestamp ).TotalDays <= 14 &&
                x.Timestamp <= toMsg.Timestamp && x.Timestamp >= fromMsg.Timestamp
            );

            await ctx.Channel.DeleteMessagesAsync( filtered );
            await ctx.RespondAsync( $"Erased: {filtered.Count()} messages, called by {ctx.User.Mention}." );
        }

        [Command( "Ban" )]
        [Description( "Bans a user with optional amount of messages to delete." )]
        [RequireUserPermissions( DSharpPlus.Permissions.BanMembers )]
        public async Task Ban( CommandContext ctx, ulong id, int deleteAmount = 0, string reason = "" )
        {
            await ctx.TriggerTypingAsync();

            await ctx.RespondAsync( "Confirm action by responding with \"yes\"." );

            InteractivityExtension interactivity = ctx.Client.GetInteractivity();
            InteractivityResult<DiscordMessage> msg = await interactivity.WaitForMessageAsync
            (
                xm => string.Equals( xm.Content, "yes",
                StringComparison.InvariantCultureIgnoreCase ),
                TimeSpan.FromSeconds( 30 )
            );

            if ( !msg.TimedOut && msg.Result.Author.Id == ctx.User.Id )
            {
                DiscordMember member = ctx.Guild.GetMemberAsync( id ).Result;

                UserProfile user = JsonConvert.DeserializeObject<UserProfile>(
                      File.ReadAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{id}.json" ) );

                user.LeaveDate = DateTime.Now;
                user.BanEntries.Add( new( DateTime.Now, reason ) );

                File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{id}.json",
                     JsonConvert.SerializeObject( user, Formatting.Indented ) );

                await ctx.Guild.BanMemberAsync( id, deleteAmount, reason );
                await ctx.RespondAsync( $"Banned {member.Mention}, deleted last {deleteAmount} messages with \"{reason}\" as reason." );
            }
            else
            {
                await ctx.RespondAsync( "Confirmation time ran out, aborting." );
            }
        }

        [Command( "Kick" )]
        [Description( "Kicks a user with an optional reason." )]
        [RequireUserPermissions( DSharpPlus.Permissions.KickMembers )]
        public async Task Kick( CommandContext ctx, ulong id, string reason = "" )
        {
            await ctx.TriggerTypingAsync();

            await ctx.RespondAsync( "Confirm action by responding with \"yes\"." );

            InteractivityExtension interactivity = ctx.Client.GetInteractivity();
            InteractivityResult<DiscordMessage> msg = await interactivity.WaitForMessageAsync
            (
                xm => string.Equals( xm.Content, "yes",
                StringComparison.InvariantCultureIgnoreCase ),
                TimeSpan.FromSeconds( 30 )
            );

            if ( !msg.TimedOut && msg.Result.Author.Id == ctx.User.Id )
            {
                UserProfile user = JsonConvert.DeserializeObject<UserProfile>(
                    File.ReadAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{id}.json" ) );

                user.LeaveDate = DateTime.Now;
                user.KickEntries.Add( new( DateTime.Now, reason ) );

                File.WriteAllText( $@"{AppDomain.CurrentDomain.BaseDirectory}ServerProfiles\{ctx.Guild.Id}UserProfiles\{id}.json",
                     JsonConvert.SerializeObject( user, Formatting.Indented ) );

                DiscordMember member = ctx.Guild.GetMemberAsync( id ).Result;
                await member.RemoveAsync();
                await ctx.RespondAsync( $"Kicked {member.Mention}, with \"{reason}\" as reason." );
            }
            else
            {
                await ctx.RespondAsync( "Confirmation time ran out, aborting." );
            }
        }

        [Command( "Unban" )]
        [Description( "Unbans a user." )]
        [RequireUserPermissions( DSharpPlus.Permissions.BanMembers )]
        public async Task Unban( CommandContext ctx, ulong id )
        {
            await ctx.TriggerTypingAsync();
            await ctx.Guild.UnbanMemberAsync( id );
            await ctx.RespondAsync( $"Unbanned {ctx.Guild.GetMemberAsync( id ).Result.Mention}." );
        }

        [Command( "ReportServers" )]
        [Description( "Responds with information on serving servers." )]
        [RequireOwner]
        public async Task ReportServers( CommandContext ctx )
        {
            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync( $"Watching {string.Join( ",\n  \t\t\t\t ", ctx.Client.Guilds.Values.ToList() )}." );
        }

        [Command( "SetStatus" )]
        [Description( "Sets the bot's status." )]
        [RequireOwner]
        public async Task SetActivity( CommandContext ctx, int type, [RemainingText] string status )
        {
            DiscordActivity activity = new DiscordActivity();
            DiscordClient discord = ctx.Client;
            activity.Name = status;
            // Offline = 0,
            // Online = 1,
            // Idle = 2,
            // DoNotDisturb = 4,
            // Invisible = 5
            await discord.UpdateStatusAsync( activity, ( UserStatus ) type, DateTimeOffset.UtcNow );
        }
    }
}
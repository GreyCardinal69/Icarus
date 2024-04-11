using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;

namespace Icarus.Modules.Other
{
    public sealed class Helpers
    {
        public static int LevenshteinDistance( string s, string t )
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];
            if ( n == 0 )
            {
                return m;
            }
            if ( m == 0 )
            {
                return n;
            }
            for ( int i = 1; i <= n; i++ )
            {
                for ( int j = 1; j <= m; j++ )
                {
                    int cost = ( t[j - 1] == s[i - 1] ) ? 0 : 1;
                    d[i, j] = Math.Min( Math.Min( d[i - 1, j] + 1, d[i, j - 1] + 1 ), d[i - 1, j - 1] + cost );
                }
            }
            return d[n, m];
        }

        public static List<string> GetAllFilesFromFolder( string root, bool searchSubfolders )
        {
            Queue<string> folders = new Queue<string>();
            List<string> files = new List<string>();
            folders.Enqueue( root );
            while ( folders.Count != 0 )
            {
                string currentFolder = folders.Dequeue();
                try
                {
                    string[] filesInCurrent = System.IO.Directory.GetFiles( currentFolder, "*.*", System.IO.SearchOption.TopDirectoryOnly );
                    files.AddRange( filesInCurrent );
                }
                catch { }
                try
                {
                    if ( searchSubfolders )
                    {
                        string[] foldersInCurrent = System.IO.Directory.GetDirectories( currentFolder, "*.*", System.IO.SearchOption.TopDirectoryOnly );
                        foreach ( string _current in foldersInCurrent )
                        {
                            folders.Enqueue( _current );
                        }
                    }
                }
                catch { }
            }
            return files;
        }

        public static void ArchiveInput( CommandContext ctx, IReadOnlyList<DiscordMessage> messages, DiscordChannel channel )
        {
            string exportPath = $"{AppDomain.CurrentDomain.BaseDirectory}Export.zip";

            if ( File.Exists( exportPath ) )
            {
                File.Delete( exportPath );
            }

            try
            {
                StringBuilder sb = new StringBuilder( Constants.ChannelExportFirstHalf.Length );

                sb.Append( @$"<!DOCTYPE html><html lang=""en""><head><title>{ctx.Guild.Name} {channel.Name}</title>" );
                sb.Append( Constants.ChannelExportFirstHalf );

                string exportsDir = $"Exports\\";
                string exportId = $"{ctx.Guild.Name.Replace( " ", "" )}{new Random().Next( 1, int.MaxValue )}";
                string fileDir = $"{exportsDir}\\{exportId}.html";
                string imagesDir = $"{exportsDir}{exportId}_Images\\";

                using WebClient client = new WebClient();
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

                sb.Append( $@"</div><div class=""postamble""><div class=""postamble__entry"">Exported {messages.Count} messages(s).</div></div>" );
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
                            string path = $"{imagesDir}\\{att.FileName}";
                            client.DownloadFile( att.Url, path );
                        }
                    }
                    string path2 = $"{imagesDir}{item.Author.Username}.jpg";
                    if ( item.Author != null && !File.Exists( path2 ) )
                    {
                        client.DownloadFile( item.Author.AvatarUrl, path2 );
                    }
                }

                ZipFile.CreateFromDirectory( @"Exports\\", exportPath );
            }
            catch ( Exception e )
            {
                Console.WriteLine( e );
            }
        }
    }
}
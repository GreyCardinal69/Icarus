using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using GreyCrammedContainer;
using Icarus.Modules.Profiles;

namespace Icarus.Modules.Isolation
{
    public class IsolationManagement : BaseCommandModule
    {
        [Command( "Isolate" )]
        [Description( "Isolates a user in a channel with specified parameters." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageRoles )]
        public async Task Isolate ( 
            CommandContext ctx,
            ulong userid,
            ulong roleid,
            ulong channelcategoryid,
            ulong backupid,
            string reason,
            string channelname,
            string isolationtime )
        {
            await ctx.TriggerTypingAsync();

            if (!Program.Core.RegisteredServerIds.Contains( ctx.Guild.Id ))
            {
                await ctx.RespondAsync( "Server is not registered, call `%RegisterServer` to proceed." );
                return;
            }

            ulong UserId = Convert.ToUInt64( userid );
            ulong PurgeRoleId = Convert.ToUInt64( roleid );
            ulong ChannelCategoryId = Convert.ToUInt64( channelcategoryid );
            ulong BackUpFileChannelId = Convert.ToUInt64( backupid );

            string Reason = ( string ) reason;
            string ChannelName = ( string ) channelname;
            // (x)d or (x)m. "d" is days, "m" is months. 
            string IsolationTime = ( string ) isolationtime;

            if (Reason == "")
            {
                await ctx.RespondAsync( "Please specify a reason." );
                return;
            }

            if (ChannelName == "")
            {
                await ctx.RespondAsync( "Please specify the isolation channel's name." );
                return;
            }

            if (IsolationTime == "")
            {
                await ctx.RespondAsync( "Please specify isolation time." );
                return;
            }

            if (ctx.Guild.GetMemberAsync( UserId ) == null)
            {
                await ctx.RespondAsync( "Invalid user id." );
                return;
            }

            if (ctx.Guild.GetRole( PurgeRoleId ) == null)
            {
                await ctx.RespondAsync( "Invalid containment role id." );
                return;
            }

            if (ctx.Guild.GetChannel( ChannelCategoryId ) == null)
            {
                await ctx.RespondAsync( "Invalid channel id." );
                return;
            }

            if (ctx.Guild.GetChannel( BackUpFileChannelId ) == null)
            {
                await ctx.RespondAsync( "Invalid backup file channel id." );
                return;
            }

            ServerProfile Profile = ServerProfile.ProfileFromId( ctx.Guild.Id );
            Profile.Entries = new();
            IsolationEntry NewEntry = new()
            {
                GuildID = ctx.Guild.Id,
                BackUpChannelID = BackUpFileChannelId,
                IsolationChannel = ChannelName,
                PurgeRoleID = PurgeRoleId,
                EntryDate = DateTimeOffset.UtcNow,
                Reason = Reason,
                ReleaseDate = DateTimeOffset.UtcNow.AddMinutes( 1 ),
                RemovedRoles = ctx.Guild.GetMemberAsync( UserId ).Result.Roles.ToList(),
                MessageID = ctx.Message.Id,
                ChannelCallID = ctx.Channel.Id,
                IsolatedUserID = userid
            };

            var member = ctx.Guild.GetMemberAsync( userid ).Result;

            foreach (var item in member.Roles.ToList())
            {
                await member.RevokeRoleAsync( item );
            }

            await member.GrantRoleAsync( ctx.Guild.GetRole( PurgeRoleId ) );

            string ProfilesPath = AppDomain.CurrentDomain.BaseDirectory + @$"\ServerProfiles\";
            Profile.Entries.Add( NewEntry );
            GccConverter.Serialize( $"{ProfilesPath}{ctx.Guild.Id}.gcc", Profile );

            await ctx.Guild.CreateChannelAsync(channelname, DSharpPlus.ChannelType.Text, ctx.Guild.GetChannel(ChannelCategoryId));
            var isolationChannel = ctx.Guild.GetChannelsAsync().Result.FirstOrDefault( x => x.Name == ChannelName );
            await isolationChannel.AddOverwriteAsync( member, DSharpPlus.Permissions.AccessChannels );

            var time = isolationtime.EndsWith( "d" )
                ? isolationtime[0..^1] + " days"
                : isolationtime[0..^1] + " months";

            await ctx.RespondAsync($"Isolated {member.Mention} at a new channel called: {channelname}" +
                $", for {time}. Removed the following roles: {string.Join(", ", NewEntry.RemovedRoles.Select(X => X.Mention))} \n" +
                $"User will be released on ({NewEntry.ReleaseDate}) +- 1-10 minutes.");
        }

        [Command( "ReleaseUser" )]
        [Description( "Releases a user from isolation." )]
        [Require​User​Permissions​Attribute( DSharpPlus.Permissions.ManageRoles )]
        public async Task ReleaseUser (CommandContext ctx, string Channel, ulong UserID)
        {
            Console.WriteLine("uwu");

            var id = ctx.Guild.Channels.Values.Where( X => X.Name == Channel ).First().Id;

            var html = new System.Net.WebClient().DownloadString( $"https://discord.com/channels/{ctx.Guild.Id/ id}" );

            File.WriteAllText( AppDomain.CurrentDomain.BaseDirectory + "Temp.txt", html);

            await ctx.Guild.GetChannel( id ).DeleteAsync();

            foreach (var role in ctx.Guild.GetMemberAsync(UserID).Result.Roles)
            {
                await ctx.Guild.GetMemberAsync( UserID ).Result.RevokeRoleAsync(role);
            }

            using (var fs = new FileStream( AppDomain.CurrentDomain.BaseDirectory + "Temp.txt", FileMode.Open, FileAccess.Read ))
            {
                await new DiscordMessageBuilder()
                          .WithFile( "Purged_Messages.txt", fs )
                               .SendAsync( ctx.Channel);
            }


        }





    }
}

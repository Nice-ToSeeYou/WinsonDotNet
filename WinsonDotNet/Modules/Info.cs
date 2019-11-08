using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using WinsonDotNet.Main;
using WinsonDotNet.Plugins;

namespace WinsonDotNet.Modules
{
    public class Info : BaseCommandModule
    {
        private Configuration Config { get; }

        public Info(IServiceProvider services)
            => Config = services.GetRequiredService<Configuration>();
        
        #region Command

        [Command("about")]
        [Description("Displays some information about Winson")]
        public async Task AboutAsync(CommandContext ctx)
            => await AboutBot(ctx);

        [Command("info")]
        [Description("Display information about a user, a role, a channel or a guild")]
        public async Task AboutObjectAsync(CommandContext ctx, DiscordMember user)
            => await AboutUserAsync(ctx, user);
        
        [Command("info")]
        [Description("Display information about a user, a role, a channel or a guild")]
        public async Task AboutObjectAsync(CommandContext ctx, DiscordRole role)
            => await AboutRoleAsync(ctx, role);
        
        [Command("info")]
        [Description("Display information about a user, a role, a channel or a guild")]
        public async Task AboutObjectAsync(CommandContext ctx, DiscordChannel channel)
            => await AboutChannelAsync(ctx, channel);
        
        [Command("info")]
        [Description("Display information about a user, a role, a channel or a guild")]
        public async Task AboutObjectAsync(CommandContext ctx, DiscordGuild guild)
            => await AboutGuildAsync(ctx, guild);

        #endregion

        #region AboutBot

        private async Task AboutBot(CommandContext ctx)
        {
            var ccv = typeof(Shards)
                          .GetTypeInfo()
                          .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                          ?.InformationalVersion ??
                      typeof(Shards)
                          .GetTypeInfo()
                          .Assembly
                          .GetName()
                          .Version
                          .ToString(3);
            
            var dsv = ctx.Client.VersionString;
            var ncv = PlatformServices.Default
                .Application
                .RuntimeFramework
                .Version
                .ToString(2);
            
            try
            {
                var a = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(xa => xa.GetName().Name == "System.Private.CoreLib");
                var pth = Path.GetDirectoryName(a?.Location);
                pth = Path.Combine(pth ?? throw new NullReferenceException($"{a?.FullName} Not found"), ".version");
                using (var fs = File.OpenRead(pth))
                using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                {
                    await sr.ReadLineAsync();    
                    ncv = await sr.ReadLineAsync();
                }
            }
            catch
            {
                // ignored
            }

            var embed = new DiscordEmbedBuilder
                {
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = $"{Config.BotName}#{ctx.Client.ShardId}",
                        Url = Config.GitHubLink,
                        IconUrl = ctx.Client.CurrentUser.AvatarUrl
                    },

                    Description =
                        $"{Config.BotName} is a bot made by **Nice to see you#6655** [<@!363862166173908992>] for the Poly Community." +
                        $"{Environment.NewLine}This bot support custom plugins in **.Net Core 2.0** made by the community." +
                        $"{Environment.NewLine}You can create your own Commands/Services and add them to the bot, just follow the guidelines on the Github Repo." +
                        $"{Environment.NewLine}[Github - Guidelines]({Config.GitHubGuidelines})",

                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "Started the "
                    },

                    Timestamp = Process.GetCurrentProcess().StartTime,

                    Color = new DiscordColor("#1ea3ff")
                }
                .AddField(":level_slider: Ram Usage", $"{Utility.GetRamUsage()}", true)
                .AddField(":robot: Bot Version", ccv, true)
                .AddField(":satellite: Latency", $"{ctx.Client.Ping:#,##0} ms", true)
                .AddField(":stopwatch: Uptime", Utility.GetUptime(), true)
                .AddField(":desktop: OS",RuntimeInformation.OSDescription.Split(new[] {'#'}, 2).FirstOrDefault(), true)
                .AddField(":floppy_disk: DSharpPlus Version", dsv, true)
                .AddField(":minidisc: .NET Core Version", ncv, true)
                .AddField(":busts_in_silhouette: Total users", ctx.Client.Guilds.SelectMany(
                    x => x.Value.Members.Where(y => !y.Value.IsBot)).Distinct().Count().ToString(), true)
                .AddField(":exclamation: Prefix", $"{Config.DiscordPrefix}", true);

            await ctx.RespondAsync(embed: embed.Build()).ConfigureAwait(false);
        }

        #endregion

        #region AboutUser

        private async Task AboutUserAsync(CommandContext ctx, DiscordMember user)
        {
            var permissions = user.PermissionsIn(ctx.Channel);
            var permString = permissions.ToString() == string.Empty ? "None" : permissions.ToPermissionString();
            
            if (((permissions & Permissions.Administrator) | (permissions & Permissions.AccessChannels)) == 0)
                permString =
                    $":warning: {Formatter.Bold("User can't see this channel!")}" +
                    $"{Environment.NewLine}{permString}";
            
            var roles = string.Empty; 
            roles = user.Roles.Any()
                ? user.Roles.Aggregate(roles, (current, role)
                    => $"{current}[{role.Name}] ")
                : "None";

            var embed = new DiscordEmbedBuilder
                {
                    Title = $"About @{user.Username}#{user.Discriminator} | ID: {user.Id} " +
                            (user.IsBot ? ":robot:" : user.IsOwner ? ":crown:" : string.Empty),

                    ThumbnailUrl = user.AvatarUrl,

                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = ctx.Guild.Name,
                        IconUrl = ctx.Guild.IconUrl
                    },

                    Timestamp = DateTimeOffset.Now,

                    Color = new DiscordColor("#1ea3ff")
                }
                .AddField("Discord Member since", user.CreationTimestamp.DateTime.ToShortDateString(), true)
                .AddField("Guild Member since", user.JoinedAt.DateTime.ToShortDateString(), true)
                .AddField("Current status", user.Presence is null ? "Offline" : user.Presence.Status.ToString(), true)
                .AddField("Current Activity", user.Presence is null
                    ? "Nothing"
                    : $"{user.Presence.Activity.ActivityType} {user.Presence.Activity.Name}", true)
                .AddField("Current Nickname", user.DisplayName, true)
                .AddField("Premium", user.PremiumType.HasValue
                    ? $"{user.PremiumType.ToString()} since {user.PremiumSince.Value.DateTime.ToShortDateString()}"
                    : "This user do not have a premium subscription")
                .AddField("Roles", roles)
                .AddField("Permissions", permString);

            await ctx.RespondAsync(embed: embed.Build()).ConfigureAwait(false);
        }

        #endregion

        #region AboutRole

        private async Task AboutRoleAsync(CommandContext ctx, DiscordRole role)
        {
            var users = string.Empty;
            users = ctx.Guild.Members.Where(user => user.Value.Roles.Contains(role))
                    .Aggregate(users, (current, user) => current + $"[{user.Value.Username}] ");

            var embed = new DiscordEmbedBuilder
                {
                    Title = $"About @{role.Name} | ID: {role.Id} ",
                    
                    Description = $"Created the {role.CreationTimestamp.DateTime.ToShortDateString()}" +
                                  $"{Environment.NewLine}Color: {role.Color.ToString()}",

                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = ctx.Guild.Name,
                        IconUrl = ctx.Guild.IconUrl
                    },

                    Timestamp = DateTimeOffset.Now,

                    Color = role.Color
                }
                .AddField("Permissions", role.Permissions.ToPermissionString())
                .AddField("Data", 
                    $"Mentionable: {(role.IsMentionable ? ":white_check_mark:" : ":x:")}" +
            $"{Environment.NewLine}Hoisted: {(role.IsHoisted ? ":white_check_mark:" : ":x:")}" +
                $"{Environment.NewLine}Managed: {(role.IsManaged ? ":white_check_mark:" : ":x:")}.")
                .AddField("Users under this role", !string.IsNullOrEmpty(users) ? users : "No one");

            await ctx.RespondAsync(embed: embed.Build()).ConfigureAwait(false);
        }

        #endregion

        #region AboutChannel

        private async Task AboutChannelAsync(CommandContext ctx, DiscordChannel channel)
        {
            var embed = new DiscordEmbedBuilder
                {
                    Title = $"About {channel.Name} | ID: {channel.Id} " +
                            (channel.IsNSFW ? ":underage:" : string.Empty),
                    
                    Description = $"Creation: {channel.CreationTimestamp.DateTime.ToShortDateString()}{Environment.NewLine}" +
                                  $"{(channel.ParentId != null ? $"Under '{channel.Parent.Name}'" : string.Empty)}",

                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = ctx.Guild.Name,
                        IconUrl = ctx.Guild.IconUrl
                    },

                    Timestamp = DateTimeOffset.Now,

                    Color = channel.IsNSFW ? new DiscordColor("#ff69b4") : new DiscordColor("#1ea3ff")
                }
                .AddField("Topic", !string.IsNullOrWhiteSpace(channel.Topic) ? channel.Topic : "No topic");
            
            if (channel.IsCategory)
            {
                var children =
                    channel.Children.Aggregate(string.Empty,
                        (current, child) => current + Utility.ReturnChannelStringType(child));
                embed.AddField("Host of", children);
            }

            if (channel.Type == ChannelType.Voice)
            {
                embed.AddField(":microphone2: Voice usage",
                    $"Bit rate: {channel.Bitrate}" +
                    $"{Environment.NewLine}" +
                    $"User limit: {(channel.UserLimit == 0 ? "Unlimited" : $"{channel.UserLimit}")}");
            }

            await ctx.RespondAsync(embed: embed.Build()).ConfigureAwait(false);
        }

        #endregion

        #region AboutGuild

        private async Task AboutGuildAsync(CommandContext ctx, DiscordGuild guild)
        {
            var embed =
                new DiscordEmbedBuilder
                    {
                        Title = $"About {guild.Name} | ID: {guild.Id}",

                        Description =
                            $"Creation: {guild.CreationTimestamp.DateTime.ToShortDateString()}{Environment.NewLine}" +
                            $"Joined: {guild.JoinedAt.DateTime.ToShortDateString()}",

                        Author = new DiscordEmbedBuilder.EmbedAuthor
                        {
                            Name = $"Owner: {guild.Owner.Username}#{guild.Owner.Discriminator}",
                            IconUrl = string.IsNullOrEmpty(guild.Owner.AvatarHash) ? null : guild.Owner.AvatarUrl
                        },

                        ThumbnailUrl = guild.IconUrl,

                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = ctx.Guild.Name, IconUrl = ctx.Guild.IconUrl
                        },

                        Timestamp = DateTimeOffset.Now,

                        Color = new DiscordColor("#1ea3ff")
                    }
                    .AddField("Members", guild.MemberCount.ToString(), true)
                    .AddField("Nitro Boosting",
                        guild.PremiumSubscriptionCount.HasValue
                            ? $"{guild.PremiumTier.ToString()} - Thanks to {guild.PremiumSubscriptionCount.Value} boosters"
                            : guild.PremiumTier.ToString(), true);
            
            foreach (var channel in guild.Channels.Values)
            {
                if (!channel.IsCategory) continue;
                var children =
                    channel.Children.Aggregate(string.Empty,
                        (current, child) => current + $"{Utility.ReturnChannelStringType(child)}" +
                                            $"{(child.Topic != null ? $": {child.Topic}" : string.Empty)}" +
                                            $@"{
                                                    (child.Type == ChannelType.Voice
                                                        ? $"Bit rate: {channel.Bitrate} User limit: " +
                                                          $"{(channel.UserLimit == 0 ? "Unlimited" : $"{channel.UserLimit}")}"
                                                        : string.Empty)
                                                }" +
                                            $"{Environment.NewLine}");
                embed.AddField(channel.Name, children);
            }

            var roles = guild.Roles.Aggregate(string.Empty, (current, role) => current + $"[{role.Value.Name}] ");
            if (!string.IsNullOrEmpty(roles))
                embed.AddField("Roles", roles);

            var emojis = guild.Emojis.Aggregate(string.Empty, (current, emoji) => current + $"{emoji.Value.Name} ");
            if (!string.IsNullOrEmpty(emojis))
                embed.AddField("Emotes", emojis);

            embed.AddField("Voice",
                $"AFK Channel: {(guild.AfkChannel != null ? $"#{guild.AfkChannel.Name}" : "None.")}" +
                $"{Environment.NewLine}AFK Timeout: {guild.AfkTimeout}{Environment.NewLine}" +
                $"Region: {guild.VoiceRegion.Name}");

            embed.AddField("Misc", $"Large: {(guild.IsLarge ? "Yes" : "No")}{Environment.NewLine}" +
                                   $"Default Notifications: {guild.DefaultMessageNotifications}{Environment.NewLine}" +
                                   $"Explicit content filter: {guild.ExplicitContentFilter}{Environment.NewLine}" +
                                   $"MFA Level: {guild.MfaLevel}{Environment.NewLine}" +
                                   $"Verification Level: {guild.VerificationLevel}");

            await ctx.RespondAsync(embed: embed.Build()).ConfigureAwait(false);
        }

        #endregion
    }
}
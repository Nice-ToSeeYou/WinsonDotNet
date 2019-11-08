using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using WinsonDotNet.Logging;
using WinsonDotNet.Main;

namespace Plugin_Economy
{
    public class Economy : BaseCommandModule
    {
        private IServiceProvider Services { get; }
        
        private UsersEco UsersEco { get; set; }
        private string NameEconomy { get; } = "Cookie";
        private string EmojiEconomy { get; } = ":cookie:";
        
        private Shop ShopList { get; set; }
        
        public Economy(IServiceProvider services)
        {
            Services = services;
        }

        #region Commands

        [Command("starteco")]
        [RequirePermissions(Permissions.BanMembers)]
        [Description("Start or stop the Economy plugin")]
        public async Task StartPluginAsync(CommandContext ctx,
            [Description("Activate the plugin or deactivate it, default false")] bool start = false)
            => await InitEventHandler(ctx, start);
        
        [Command("showshop")]
        [Aliases("shop")]
        [Description("Start or stop the Economy plugin")]
        public async Task ShowShopAsync(CommandContext ctx)
            => await ShowShop(ctx);
        
        [Command("eco")]
        [Description("Show how rich you are")]
        public async Task ShowEcoAsync(CommandContext ctx)
            => await ShowEco(ctx);

        #endregion

        #region EventHandler

        private async Task InitEventHandler(CommandContext ctx, bool start)
        {
            if (start)
            {
                await Services.GetRequiredService<Logger>()
                    .LogAsync(new LogMessage(LogLevel.Info, "Economy plugin", $"The module was activated in {ctx.Guild.Name}"));
                ctx.Client.MessageCreated += ClientOnMessageCreated;
                ctx.Client.MessageReactionAdded += ClientOnMessageReactionAdded;
                ctx.Client.MessageReactionRemoved += ClientOnMessageReactionRemoved;

                UsersEco = await GetUsers();
                ShopList = await GetShopObjects();

                var embed = new DiscordEmbedBuilder
                {
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = ctx.User.Username,
                        IconUrl = ctx.User.AvatarUrl
                    },
                    
                    Description = ":white_check_mark: The Economy plugin is now active",
                    
                    Timestamp = DateTimeOffset.Now,
                    
                    Color = DiscordColor.Green
                };
                
                await ctx.RespondAsync(embed: embed.Build());
            }
            else
            {
                await Services.GetRequiredService<Logger>()
                    .LogAsync(new LogMessage(LogLevel.Info, "Economy plugin", $"The module was deactivated in {ctx.Guild.Name}"));
                ctx.Client.MessageCreated -= ClientOnMessageCreated;
                ctx.Client.MessageReactionAdded -= ClientOnMessageReactionAdded;
                ctx.Client.MessageReactionRemoved -= ClientOnMessageReactionRemoved;
                
                var embed = new DiscordEmbedBuilder
                {
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = ctx.User.Username,
                        IconUrl = ctx.User.AvatarUrl
                    },
                    
                    Description = ":x: The Economy plugin is now inactive",
                    
                    Timestamp = DateTimeOffset.Now,
                    
                    Color = DiscordColor.Red
                };
                
                await ctx.RespondAsync(embed: embed.Build());
            }
        }

        private async Task ClientOnMessageCreated(MessageCreateEventArgs e)
        {
            if (e.Author.IsBot) return;

            if (UsersEco.Users.Exists(u => u.Id == e.Author.Id))
            {
                var user = UsersEco.Users.Find(u => u.Id == e.Author.Id);
                if (user.Time > DateTime.Now) return;
                
                AddCookieToUser(e.Author);
            }
            else
                AddCookieToUser(e.Author);
            
            await SaveEcoModule();
        }

        private async Task ClientOnMessageReactionAdded(MessageReactionAddEventArgs e)
        {
            if (e.User.IsBot || e.Message.Author.IsBot || e.User == e.Message.Author) return;
            if (e.Emoji.GetDiscordName() != EmojiEconomy) return;
            
            AddCookieToUser(e.Message.Author);
            
            await SaveEcoModule();
            await e.Channel.SendMessageAsync(
                $"**{e.User.Username}** you gave one {NameEconomy} to **{e.Message.Author.Username}**");
        }

        private async Task ClientOnMessageReactionRemoved(MessageReactionRemoveEventArgs e)
        {
            if (e.User.IsBot || e.Message.Author.IsBot || e.User == e.Message.Author) return;
            if (e.Emoji.GetDiscordName() != EmojiEconomy) return;
            
            AddCookieToUser(e.Message.Author, -1);
            
            await SaveEcoModule();
            await e.Channel.SendMessageAsync(
                $"**{e.User.Username}** you removed one {NameEconomy} to **{e.Message.Author.Username}**");
        }

        #endregion

        #region ShowShop

        private async Task ShowShop(CommandContext ctx)
        {
            var embed = new DiscordEmbedBuilder
            {
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = $"{ctx.Member.DisplayName}",
                    IconUrl = ctx.Member.AvatarUrl
                },

                Title = $"Shop items in {ctx.Guild.Name}'s Guild",

                Description = $"To buy an object, click on the reaction corresponding to the object or type:" +
                              $"{Environment.NewLine}**{Services.GetRequiredService<Configuration>().DiscordPrefix}buy [NameOfTheObject]**",

                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = ctx.Guild.Name,
                    IconUrl = ctx.Guild.IconUrl
                },

                Timestamp = DateTimeOffset.Now,

                Color = new DiscordColor("#1ea3ff")
            };
            
            foreach (var obj in ShopList.Objects)
                embed.AddField($"{obj.Name} - {obj.Emoji}",
                    $"{obj.Description}{Environment.NewLine}You can buy it for {obj.Price} {EmojiEconomy}", true);

            var mes = await ctx.RespondAsync(embed: embed.Build());

            foreach (var obj in ShopList.Objects)
                await mes.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, obj.Emoji));
            
            Reaction:
            var react = await ctx.Client.GetInteractivity().WaitForReactionAsync(mes, ctx.User);
            if (!react.TimedOut)
            {
                foreach (var obj in ShopList.Objects.Where(obj => react.Result.Emoji.GetDiscordName() == obj.Emoji))
                {
                    if (UsersEco.Users.Exists(u => u.Id == react.Result.User.Id))
                    {
                        var user = UsersEco.Users.Find(u => u.Id == react.Result.User.Id);
                        if (user.Cookies >= obj.Price)
                        {
                            AddCookieToUser(react.Result.User, -obj.Price);
                            await ctx.RespondAsync($"**{react.Result.User.Username}** bought {obj.Name}");
                            return;
                        }

                        await ctx.RespondAsync($"Sorry **{react.Result.User.Username}** you do not have enough {NameEconomy}s");
                        goto Reaction;
                    }

                    AddCookieToUser(react.Result.User, -obj.Price);
                    await ctx.RespondAsync($"Sorry **{react.Result.User.Username}** you do not have enough {NameEconomy}s");
                    return;
                }
            }
        }

        #endregion

        #region ShowEco

        private async Task ShowEco(CommandContext ctx)
        {
            if (!UsersEco.Users.Exists(u => u.Id == ctx.User.Id))
                AddCookieToUser(ctx.User);
            
            var user = UsersEco.Users.Find(u => u.Id == ctx.User.Id); 
            
            var embed = new DiscordEmbedBuilder
            {
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = ctx.Member.DisplayName,
                    IconUrl = ctx.Member.AvatarUrl
                },
                
                Description = $"You have {user.Cookies} {NameEconomy} {EmojiEconomy}",
                
                Color = ctx.Member.Color
            };

            await ctx.RespondAsync(embed: embed.Build());
        }

        #endregion

        #region Utility

        private async Task<UsersEco> GetUsers()
        {
            var ecoFile = new FileInfo("userseco.json");

            if (!ecoFile.Exists)
            {
                await Services.GetRequiredService<Logger>()
                    .LogAsync(new LogMessage(LogLevel.Warning, "Plugin - Economy",
                        "A new Json file was created for the plugin"));

                var ecoJson = UsersEco.Default.ToJson();
                using (var ecoCreate = ecoFile.Create())
                using (var streamWriter = new StreamWriter(ecoCreate, Encoding.BigEndianUnicode))
                {
                    await streamWriter.WriteAsync(ecoJson);
                    await streamWriter.FlushAsync();
                }

                return UsersEco.Default;
            }

            var fileStream = ecoFile.OpenRead();
            var streamReader = new StreamReader(fileStream, Encoding.BigEndianUnicode);

            var jsonReadToEnd = await streamReader.ReadToEndAsync();
            streamReader.Close();
            return UsersEco.FromJson(jsonReadToEnd);
        }
        
        private async Task<Shop> GetShopObjects()
        {
            var objectFile = new FileInfo("shopobject.json");

            if (!objectFile.Exists)
            {
                await Services.GetRequiredService<Logger>()
                    .LogAsync(new LogMessage(LogLevel.Warning, "Plugin - Economy",
                        "A new Json file was created for the plugin"));

                var objectJson = Shop.Default.ToJson();
                using (var objectCreate = objectFile.Create())
                using (var streamWriter = new StreamWriter(objectCreate, Encoding.BigEndianUnicode))
                {
                    await streamWriter.WriteAsync(objectJson);
                    await streamWriter.FlushAsync();
                }

                return Shop.Default;
            }

            var fileStream = objectFile.OpenRead();
            var streamReader = new StreamReader(fileStream, Encoding.BigEndianUnicode);

            var jsonReadToEnd = await streamReader.ReadToEndAsync();
            streamReader.Close();
            return Shop.FromJson(jsonReadToEnd);
        }
        
        private void AddCookieToUser(DiscordUser discordUser, long cookie = 1)
        {
            if (UsersEco.Users.Exists(u => u.Id == discordUser.Id))
            {
                var user = UsersEco.Users.Find(u => u.Id == discordUser.Id);
                
                var rand = new Random();
                
                UsersEco.Users.Remove(user);
                user.Cookies += cookie;
                user.Time = DateTime.Now.Add(TimeSpan.FromMinutes(rand.Next(10, 21)));
                UsersEco.Users.Add(user);
            }
            else
            {
                var user = new User
                {
                    Id = discordUser.Id,
                    Username = discordUser.Username,
                    Cookies = cookie,
                    Time = DateTime.Now
                };
                
                UsersEco.Users.Add(user);
            }
        }

        private async Task SaveEcoModule()
        {
            var ecoFile = new FileInfo("userseco.json");
            
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Debug, "Plugin - Economy",
                    "Data were saved"));
            
            var ecoJson = UsersEco.ToJson();
            using (var ecoWrite = ecoFile.OpenWrite())
            using (var streamWriter = new StreamWriter(ecoWrite, Encoding.BigEndianUnicode))
            {
                await streamWriter.WriteAsync(ecoJson);
                await streamWriter.FlushAsync();
            }
        }

        #endregion
    }

    internal partial class UsersEco
    {
        [JsonProperty("Users")]
        public List<User> Users { get; set; }
    }

    internal class User
    {
        [JsonProperty("ID")]
        public ulong Id { get; set; }

        [JsonProperty("Username")]
        public string Username { get; set; }

        [JsonProperty("Cookies")]
        public long Cookies { get; set; }

        [JsonProperty("Time")]
        public DateTime Time { get; set; }
    }

    internal partial class UsersEco
    {
        public static UsersEco FromJson(string json) => JsonConvert.DeserializeObject<UsersEco>(json);

        public static UsersEco Default => new UsersEco {Users = new List<User>()};
    }

    internal static class Serialize
    {
        public static string ToJson(this UsersEco self) => JsonConvert.SerializeObject(self);
        public static string ToJson(this Shop self) => JsonConvert.SerializeObject(self);
    }

    public partial class Shop
    {
        [JsonProperty("Objects")] public List<Object> Objects { get; set; }
    }

    public class Object
    {
        [JsonProperty("Name")] public string Name { get; set; }

        [JsonProperty("Description")] public string Description { get; set; }

        [JsonProperty("Emoji")] public string Emoji { get; set; }

        [JsonProperty("Price")] public long Price { get; set; }
    }

    public partial class Shop
    {
        public static Shop FromJson(string json) => JsonConvert.DeserializeObject<Shop>(json);

        public static Shop Default => new Shop {Objects = new List<Object>()};
    }
}
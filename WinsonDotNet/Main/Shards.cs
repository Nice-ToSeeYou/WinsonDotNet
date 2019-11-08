using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using WinsonDotNet.Handlers;
using WinsonDotNet.Logging;
using WinsonDotNet.Plugins;

namespace WinsonDotNet.Main
{
    public class Shards
    {
        // Overall variables
        private static Configuration Config { get; set; }
        private static List<string> Plugins { get; set; }
        private static IServiceProvider Services { get; set; }

        /// <summary>
        /// Start asynchronously sharding, it use the Automated sharded client in DSharpPlus.
        /// </summary>
        public static async Task StartSharding()
        {
            await Logger.LogStatic(new LogMessage(LogLevel.Info, "Winson - Sharding", "Starting Winson sharding"));
            
            await Logger.LogStatic(new LogMessage(LogLevel.Info, "Winson - Sharding", "Loading Configuration"));
            Config = await GetOrCreateConfig();
            
            if (Config == null)
                throw new ApplicationException("A new configuration file was created and need to be filed");
            if (Config.DiscordToken == "[Insert token here]" || string.IsNullOrWhiteSpace(Config.DiscordToken))
                throw new ApplicationException("The discord token is either not specified or not right please check it");
            
            // Create the service collection that will be used inside the CommandNext module
            Services = new ServiceCollection()
                .AddSingleton(new DiscordShardedClient(new DiscordConfiguration
                {
                    AutoReconnect = Config.AutoReconnect,
                    ReconnectIndefinitely = false,

                    DateTimeFormat = Config.DateTimeFormat,

                    GatewayCompressionLevel = GatewayCompressionLevel.Stream,
                    HttpTimeout = TimeSpan.FromSeconds(Config.HttpTimeout),

                    LargeThreshold = Config.LargeThreshold,
                    MessageCacheSize = Config.MessageCacheSize,

                    LogLevel = LogLevel.Debug,
                    UseInternalLogHandler = Config.UseInternalLogHandler,

                    Token = Config.DiscordToken,
                    TokenType = TokenType.Bot
                }))
                .AddSingleton(Config)
                .AddSingleton<Logger>()
                .AddSingleton<Clients>()
                .AddSingleton<Commands>()
                .AddSingleton<Utility>()
                .BuildServiceProvider();
            
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Info, "Winson - Sharding", "Configuration fully loaded"));
            
            // Seek for all plugins
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Info, "Winson - Sharding", "Loading Plugins"));
            Plugins = await Loader.GetPlugins(Services);
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Info, "Winson - Sharding", "All Plugins were loaded"));

            // Boot the bot
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Info, "Winson - Sharding", "Booting Winson"));
            await BootAsync();
        }

        /// <summary>
        /// Make the interactivity and the Command next module, finally launch all the shards.
        /// </summary>
        private static async Task BootAsync()
        {
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Info, "Winson - Boot", "Starting client"));
            
            // Launch events inside the Client module
            Services.GetRequiredService<Clients>().InitEventHandler();

            // Start the Interactivity and CommandNext used by the Client
            await Services.GetRequiredService<DiscordShardedClient>().UseInteractivityAsync(
                new InteractivityConfiguration
                {
                    PaginationBehaviour = PaginationBehaviour.WrapAround,
                    PaginationDeletion = PaginationDeletion.DeleteEmojis,

                    PollBehaviour = PollBehaviour.DeleteEmojis,
                    Timeout = TimeSpan.FromMinutes(Config.Timeout)
                });
            
            await Services.GetRequiredService<DiscordShardedClient>().UseCommandsNextAsync(new CommandsNextConfiguration
            {
                CaseSensitive = Config.CaseSensitive,
                DmHelp = Config.DmHelp,
                EnableDms = Config.EnableDms,
                
                EnableDefaultHelp = Config.EnableDefaultHelp,
                EnableMentionPrefix = Config.EnableMentionPrefix,
                IgnoreExtraArguments = Config.IgnoreExtraArguments,
                PrefixResolver = PrefixResolverAsync,
                
                Services = Services
            });

            // Launch events inside the Commands Handler
            await Services.GetRequiredService<Commands>().InitEventHandler(Plugins);
            
            // Start all shards and make them never stop
            await Services.GetRequiredService<DiscordShardedClient>().StartAsync().ConfigureAwait(false);
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Info, "Winson - Boot", "Winson is now fully started"));
            await Task.Delay(-1);
        }
        
        /// <summary>
        /// Create or return the Configuration information hold inside the config.json file.
        /// </summary>
        /// <returns>
        /// Return the configuration details.
        /// </returns>
        private static async Task<Configuration> GetOrCreateConfig()
        {
            var configFile = new FileInfo("config.json");

            // Look if the config.json file exist, if not create a new one
            if (!configFile.Exists)
            {
                await Logger.LogStatic(new LogMessage(LogLevel.Error, "Winson - Configuration",
                    "Loading configuration failed: config.json does not exist"));
                
                var configJson = JsonConvert.SerializeObject(Configuration.Default, Formatting.Indented);
                using(var configCreate = configFile.Create())
                using (var streamWriter = new StreamWriter(configCreate, Encoding.BigEndianUnicode))
                {
                    await streamWriter.WriteAsync(configJson);
                    await streamWriter.FlushAsync();
                }

                await Logger.LogStatic(new LogMessage(LogLevel.Warning, "Winson - Configuration",
                    $"A new default configuration file has been written to the following location: " +
                    $"{Environment.NewLine}{configFile.FullName}" +
                    $"{Environment.NewLine}Fill it, then re-run this program. Press any key to close the program."));

                Console.ReadKey(true);
                return null;
            }
            
            // Read the file
            var fileStream = configFile.OpenRead();
            var streamReader = new StreamReader(fileStream, Encoding.BigEndianUnicode);

            var jsonReadToEnd = await streamReader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<Configuration>(jsonReadToEnd);
        }
        
        /// <summary>
        /// Resolve the discord prefix used for all the shard.
        /// </summary>
        /// <param name="m"> The discord message that triggered the action.</param>
        /// <returns>
        /// Return an int stating if the prefix used is the right one or not.
        /// </returns>
        private static Task<int> PrefixResolverAsync(DiscordMessage m)
        {
            if (m.GetStringPrefixLength(Config.DiscordPrefix) == Config.DiscordPrefix.Length &&
                m.Content.StartsWith(Config.DiscordPrefix))
                return Task.FromResult(m.GetStringPrefixLength(Config.DiscordPrefix));

            return Task.FromResult(-1);
        }
    }
}
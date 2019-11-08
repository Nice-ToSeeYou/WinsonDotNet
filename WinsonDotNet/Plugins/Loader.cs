using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;
using WinsonDotNet.Logging;

namespace WinsonDotNet.Plugins
{
    public class Loader
    {
        private const string PluginsPath = @"Plugins/";
        
        /// <summary>
        /// Look inside the Plugins folder for any file that match the BaseCommandModule to create the commands.
        /// </summary>
        /// <param name="services"> The service provider needed to log the data.</param>
        /// <returns>
        /// Return a list of Plugin Name (path to the file) to load them.
        /// </returns>
        public static async Task<List<string>> GetPlugins(IServiceProvider services)
        {
            // Look if the folder exist if not creat one
            if (!Directory.Exists(PluginsPath)) Directory.CreateDirectory(PluginsPath);
            var directoryInfo = new DirectoryInfo(PluginsPath);

            // If no plugin just return null
            if (!directoryInfo.Exists)
            {
                await services.GetRequiredService<Logger>()
                    .LogAsync(new LogMessage(LogLevel.Error, "Winson - Plugins", "No plugins were found"));
                return null;
            }
            
            // If some files exists, we look at there extension to be sure it is in .dll
            // and after we verify the type of the plugin is really a command made for DSharpPlus
            var plugins = new List<string>();

            foreach (var file in directoryInfo.GetFiles("*.dll"))
            {
                var plugin = Assembly.LoadFile(file.FullName);

                foreach (var t in plugin.GetTypes())
                {
                    if (!typeof(BaseCommandModule).IsAssignableFrom(t)) continue;
                    
                    plugins.Add(file.FullName);
                    await services.GetRequiredService<Logger>()
                        .LogAsync(new LogMessage(LogLevel.Warning, "Winson - Plugins", $"Plugin : {t.Module.Name} loaded"));
                }
            }
            
            // Return the list
            return await Task.FromResult(plugins);
        }
    }
}
using System;
using System.Diagnostics;
using DSharpPlus;
using DSharpPlus.Entities;

namespace WinsonDotNet.Plugins
{
    public class Utility
    {
        /// <summary>
        /// Get the RAM usage of the program.
        /// </summary>
        /// <returns>
        /// Return the string holding the data.
        /// </returns>
        public static string GetRamUsage()
        {
            var memory = GC.GetTotalMemory(false);
            return $"{FormatRamValue(memory):f2} {FormatRamUnit(memory)}";
        }
        
        /// <summary>
        /// Get the UpTime of the program.
        /// </summary>
        /// <returns>
        /// Return the string holding the data.
        /// </returns>
        public static string GetUptime()
        {
            var uptime = (DateTime.Now - Process.GetCurrentProcess().StartTime);
            return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
        }
        
        /// <summary>
        /// Get the type of a Discord Channel.
        /// </summary>
        /// <param name="channel"> The Discord channel.</param>
        /// <returns>
        /// Return a string of the type of the specified channel.
        /// </returns>
        public static string ReturnChannelStringType(DiscordChannel channel)
        {
            switch (channel.Type)
            {
                case ChannelType.Text:
                    return Formatter.Bold($"[#{channel.Name} :speech_balloon:] ");

                case ChannelType.Voice:
                    return Formatter.Bold($"[{channel.Name} :loudspeaker:] ");

                case ChannelType.Category:
                    return Formatter.Bold($"[{channel.Name.ToUpper()} :file_folder:] ");

                default:
                    return Formatter.Bold($"[{channel.Name} :grey_question:] ");
            }
        }
        
        /// <summary>
        /// Format the RAM value to a readable state.
        /// </summary>
        /// <param name="mem"> The memory used.</param>
        /// <returns>
        /// Return a readable long.
        /// </returns>
        private static long FormatRamValue(long mem)
        {
            while (mem > 1000) mem /= 1000;
            return mem;
        }
        
        /// <summary>
        /// Format the RAM value to a readable unit.
        /// </summary>
        /// <param name="mem"> The memory used.</param>
        /// <returns>
        /// Return a unit according to the current RAM state.
        /// </returns>
        private static string FormatRamUnit(long mem)
        {
            var units = new[] {"B", "KB", "MB", "GB", "TB", "PB"};
            var unitCount = 0;
            while (mem > 1000)
            {
                mem /= 1000;
                unitCount++;
            }
            return units[unitCount];
        }
    }
}
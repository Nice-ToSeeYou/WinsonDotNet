using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace WinsonDotNet.Modules
{
    public class Ping : BaseCommandModule
    {
        #region Command

        [Command("ping")]
        [Description("Displays this shard's WebSocket latency, and an optional host if specified")]
        public async Task PingAsync(CommandContext ctx,
            [Description("Optional, a specific host you want to ping, default 'google.com'")] string host = "google.com")
            => await ShowPing(ctx, host);

        #endregion

        #region ShowPing

        private static async Task ShowPing(CommandContext ctx, string host)
        {
            DiscordEmbedBuilder emb;
            
            try
            {
                var pingLocal = await new System.Net.NetworkInformation.Ping().SendPingAsync("localhost")
                    .ConfigureAwait(false);
                var pingDistant = await new System.Net.NetworkInformation.Ping().SendPingAsync(host)
                    .ConfigureAwait(false);

                emb = new DiscordEmbedBuilder()
                    .WithTitle(":ping_pong: Current latency")
                    .AddField($":earth_africa: {host}", $"{pingDistant.Status}: {pingDistant.RoundtripTime:#,##0} ms",
                        true)
                    .AddField(":satellite: WebSocket", $"Success: {ctx.Client.Ping:#,##0} ms", true)
                    .AddField(":house: LocalHost", $"{pingLocal.Status}: {pingLocal.RoundtripTime:#,##0} ms", true)
                    .WithColor(new DiscordColor("#1ea3ff"));
            }
            catch (Exception e)
            {
                emb = new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Description = $"{e.TargetSite.Name}{Environment.NewLine}{e.Message}",
                    Color = new Optional<DiscordColor>(new DiscordColor("#e60000"))
                };
            }
            
            await ctx.RespondAsync(embed: emb);
        }

        #endregion
    }
}
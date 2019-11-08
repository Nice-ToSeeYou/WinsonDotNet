using System;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using WinsonDotNet.Logging;

namespace WinsonDotNet.Handlers
{
    public class Clients
    {
         private DiscordShardedClient Client { get; }
         private IServiceProvider Services { get; }
        
        public Clients(IServiceProvider serviceProvider)
        {
            Client = serviceProvider.GetRequiredService<DiscordShardedClient>();
            Services = serviceProvider;
        }
        
        public void InitEventHandler()
        {
            Client.SocketOpened += OnSocketOpened;
            Client.SocketErrored += OnSocketErrored;
            Client.SocketClosed += OnSocketClosed;
            Client.WebhooksUpdated += OnWebhooksUpdated;
            
            Client.Ready += OnClientReady;
            Client.Resumed += OnClientResumed;
            Client.ClientErrored += OnClientErrored;
            Client.Heartbeated += OnHeartbeat;

            Client.GuildCreated += OnGuildCreated;
            Client.GuildAvailable += OnGuildAvailable;
            Client.GuildUnavailable += OnGuildUnavailable;
            Client.GuildDeleted += OnGuildDeleted;

            Client.UnknownEvent += OnUnknownEvent;
        }

        private async Task OnUnknownEvent(UnknownEventArgs e)
        {
            var file = $"{Guid.NewGuid()}_{e.EventName}.json";
            var message = $"An unknown event happened: {e.EventName}{Environment.NewLine}" +
                          $"It was saved with the name {file}";
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Critical, $"Winson {e.Client.ShardId}", message));
            File.AppendAllText(file, e.Json);
        }

        #region Socket

        private async Task OnSocketOpened()
        {
            const string message = "Socket opened";
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Debug, "Socket", message));
        }
        
        private async Task OnSocketErrored(SocketErrorEventArgs e)
        {
            var message = "Socket error";
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Error, $"Winson {e.Client.ShardId}", message, e.Exception));
        }
        
        private async Task OnSocketClosed(SocketCloseEventArgs e)
        {
            var message = $"Socket closed with the code {e.CloseCode}: {e.CloseMessage}";
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Debug, $"Winson {e.Client.ShardId}", message));
        }
        
        private async Task OnWebhooksUpdated(WebhooksUpdateEventArgs e)
        {
            var message = $"Webhook was updated in this channel {e.Channel.Name} - {e.Channel.Id}";
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Debug, $"Winson {e.Client.ShardId} - {e.Guild.Id}", message));
        }

        #endregion
    
        #region Client

        private async Task OnClientReady(ReadyEventArgs e)
        {
            await Client.UpdateStatusAsync(new DiscordActivity("I am here to help!", ActivityType.ListeningTo),
                UserStatus.Online);
            
            const string message = "Client is ready to process events";
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Info, $"Winson {e.Client.ShardId}", message));
        }
        
        private async Task OnClientResumed(ReadyEventArgs e)
        {
            await Client.UpdateStatusAsync(new DiscordActivity("I am here to help!", ActivityType.ListeningTo),
                UserStatus.Online);
            
            const string message = "Client has resumed";
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Debug, $"Winson {e.Client.ShardId}", message));
        }
        
        private async Task OnClientErrored(ClientErrorEventArgs e)
        {
            var message = $"Exception occured: {e.EventName}";

            if (e.Exception.InnerException != null)
            {
                message = $"Inner exception: {e.Exception.InnerException.GetType()}: " +
                          $"{e.Exception.InnerException.Message}";
                await Services.GetRequiredService<Logger>()
                    .LogAsync(new LogMessage(LogLevel.Error, $"Winson {e.Client.ShardId}", message, e.Exception));
            }
            else
                await Services.GetRequiredService<Logger>()
                    .LogAsync(new LogMessage(LogLevel.Error, $"Winson {e.Client.ShardId}", message, e.Exception));
        }
        
        private async Task OnHeartbeat(HeartbeatEventArgs e)
        {
            var message = $"Heartbeat ping: {e.Ping} checksum: {e.IntegrityChecksum}";
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Debug, $"Winson {e.Client.ShardId}", message));
        }

        #endregion

        #region Guild

        private async Task OnGuildCreated(GuildCreateEventArgs e)
        {
            var message = $"New guild available, {e.Guild.Name} #{e.Guild.Id}, " +
                          $"owner: {e.Guild.Owner.Username} #{e.Guild.Owner.Discriminator}";
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Warning, $"Winson {e.Client.ShardId}", message));
        }
        
        private async Task OnGuildAvailable(GuildCreateEventArgs e)
        {
            var message = $"Guild available: {e.Guild.Name} [{e.Guild.Id}]";
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Debug, $"Winson {e.Client.ShardId}", message));
        }
        
        private async Task OnGuildUnavailable(GuildDeleteEventArgs e)
        {
            var message = $"Guild unavailable: {e.Guild.Name} [{e.Guild.Id}], " +
                          $"owner:{e.Guild.Owner.Username}#{e.Guild.Owner.Discriminator}";
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Debug, $"Winson {e.Client.ShardId}", message));
        }
        
        private async Task OnGuildDeleted(GuildDeleteEventArgs e)
        {
            var message = $"Guild deleted, {e.Guild.Name} #{e.Guild.Id}, " +
                          $"owner: {e.Guild.Owner.Username} #{e.Guild.Owner.Discriminator}";
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Warning, $"Winson {e.Client.ShardId}", message));
        }

        #endregion
    }
}
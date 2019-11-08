using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using WinsonDotNet.Logging;

namespace WinsonDotNet.Handlers
{
    public class Commands
    {
        private IServiceProvider Services { get; }
        
        public Commands(IServiceProvider serviceProvider)
        {
            Services = serviceProvider;
        }

        public async Task InitEventHandler(List<string> plugins)
        {
            foreach (var socket in Services.GetRequiredService<DiscordShardedClient>().GetCommandsNext().Values)
            {
                await Services.GetRequiredService<Logger>()
                    .LogAsync(new LogMessage(LogLevel.Info, $"Winson {socket.Client.ShardId}", "Loading commands"));
                
                socket.SetHelpFormatter<Help>();
                socket.RegisterCommands(Assembly.GetExecutingAssembly());

                if (plugins.Count > 0)
                    foreach (var plugin in plugins)
                        socket.RegisterCommands(Assembly.LoadFrom(plugin));

                socket.CommandExecuted += Command_Executed;
                socket.CommandErrored += Command_Error;
            }
        }
        
        private async Task Command_Executed(CommandExecutionEventArgs e)
        {
            var user = e.Context.Member.Nickname ?? e.Context.Member.DisplayName;
            var message =
                $"{user}#{e.Context.Member.Discriminator} successfully executed '{e.Command.QualifiedName}'";
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Debug, $"Winson {e.Context.Client.ShardId} - {e.Context.Guild.Name}", message));
        }

        private async Task Command_Error(CommandErrorEventArgs e)
        {
            var user = e.Context.Member.Nickname ?? e.Context.Member.DisplayName;
            var message = $"{user}#{e.Context.Member.Discriminator} tried executing " +
                          $"'{e.Command?.QualifiedName ?? "<unknown command>"}' but an error occured";
            
            await Services.GetRequiredService<Logger>()
                .LogAsync(new LogMessage(LogLevel.Warning, $"Winson {e.Context.Client.ShardId} - {e.Context.Guild.Name}", message, e.Exception));
            
            var answer = new List<string>();
            switch (e.Exception)
            {
                case ChecksFailedException ex:

                    foreach (var error in ex.FailedChecks)
                        switch (error)
                        {
                            case RequireBotPermissionsAttribute attribute:
                                answer.Add("Sorry but I need the following permission(s):" +
                                           $" {attribute.Permissions.ToPermissionString()}{Environment.NewLine}");
                                break;

                            case RequireOwnerAttribute _:
                                answer.Add("You are not the owner of the bot, you cannot use this command" +
                                           $"{Environment.NewLine}");
                                break;

                            case RequireRolesAttribute attribute:
                                answer.Add("You need the following role(s) in order to use this command:" +
                                           $" {string.Join(", ", attribute.RoleNames)}{Environment.NewLine}");
                                break;

                            case RequireUserPermissionsAttribute attribute:
                                answer.Add("Sorry but you need the following permission(s): " +
                                           $"{attribute.Permissions.ToPermissionString()}{Environment.NewLine}");
                                break;
                            
                            case RequirePrefixesAttribute attribute:
                                answer.Add("Sorry but you need to use one of the following prefix(es) to use this command: " +
                                           $"{string.Join(", ", attribute.Prefixes)}{Environment.NewLine}");
                                break;

                            case CooldownAttribute attribute:
                                answer.Add("Are you Flash? You need to wait at least: " +
                                           $"{attribute.GetRemainingCooldown(e.Context).Seconds} s{Environment.NewLine}");
                                break;
                            
                            case RequireNsfwAttribute _:
                                answer.Add($":underage: You need to be inside a NSFW channel!{Environment.NewLine}");
                                break;
                            
                            case RequirePermissionsAttribute attribute:
                                answer.Add("Sorry but following permission(s) are needed: " +
                                           $"{attribute.Permissions.ToPermissionString()}{Environment.NewLine}");
                                break;
                        }
                    break;

                case UnauthorizedException _:
                    answer.Add(
                        "I don't have enough power to perform this action " +
                        "(please check the hierarchy of the bot is correct)");
                    break;

                case BadRequestException _:
                    answer.Add("Discord stopped the action");
                    break;

                default:
                    await Services.GetRequiredService<Logger>()
                        .LogAsync(new LogMessage(LogLevel.Error, $"Winson {e.Context.Client.ShardId} - {e.Context.Guild.Name}",
                        "An unknown error occured", e.Exception));
                    break;
            }

            if (answer.Count > 0)
            {
                var embed = new DiscordEmbedBuilder()
                {
                    Author = new DiscordEmbedBuilder.EmbedAuthor()
                    {
                        IconUrl = e.Context.User.AvatarUrl,
                        Name = e.Context.Member.DisplayName
                    },
                    Color = new Optional<DiscordColor>(new DiscordColor("#890032")),
                    Title = "An error occured"
                };
                
                embed
                    .AddField(Formatter.Underline("Command"), e.Command is null ? "<Unknown Command>" : e.Command.QualifiedName, true)
                    .AddField(Formatter.Underline("Error(s)"), string.Join(" | ", answer));

                await e.Context.RespondAsync(embed: embed.Build());
            }
        }
    }
}
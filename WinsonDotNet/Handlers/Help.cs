using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;

namespace WinsonDotNet.Handlers
{
    public class Help: BaseHelpFormatter
    {
        private DiscordEmbedBuilder Embed { get; }
        private Command Command { get; set; }
        
        public Help(CommandContext ctx) : base(ctx)
        {
            Embed = new DiscordEmbedBuilder()
            {
                Title = "Winson Help",
                
                Description = "You can found further help with the " +
                              "[wiki]()" +
                              $"{Environment.NewLine}",
                
                Color = new DiscordColor("#1ea3ff")
            };
        }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            Command = command;    

            Embed.Description +=
                // ReSharper disable once UseStringInterpolation
                string.Format("{0}{1}: {2}", Environment.NewLine, Formatter.InlineCode(command.Name),
                    (Enumerable.Contains(command.Description, ']')
                        ? command.Description.Substring(command.Description.IndexOf(']') + 1)
                        : command.Description) ?? "No description provided");

            if (command is CommandGroup cGroup && cGroup.IsExecutableWithoutSubcommands)
                Embed.WithFooter("This group can be executed as a standalone command");

            if (command.Aliases?.Any() == true)
                Embed.AddField("Aliases", string.Join(", ", command.Aliases.Select(Formatter.InlineCode)));

            if (command.Overloads?.Any() != true) return this;
            var sb = new StringBuilder();

            foreach (var ovl in command.Overloads.OrderByDescending(x => x.Priority))
            {
                sb.Append('`').Append($"Full Command: {command.QualifiedName}");

                foreach (var arg in ovl.Arguments)
                    sb.Append(arg.IsOptional || arg.IsCatchAll ? " [" : " <").Append(arg.Name)
                        .Append(arg.IsCatchAll ? "..." : "").Append(arg.IsOptional || arg.IsCatchAll ? ']' : '>');

                sb.Append($"`{Environment.NewLine}");

                foreach (var arg in ovl.Arguments)
                    sb.Append('`').Append(arg.Name).Append(" (").Append(CommandsNext.GetUserFriendlyTypeName(arg.Type)).Append(")`: ")
                        .Append(arg.Description ?? "No description provided.").Append(Environment.NewLine);

                sb.Append(Environment.NewLine);
            }

            Embed.AddField("Arguments", sb.ToString().Trim());

            return this;
        }

        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subCommands)
        {
            var subgroups = new List<(string, string)>();
            foreach (var cmd in subCommands)
            {
                if (cmd.Description != null && cmd.Description.StartsWith("[") && Enumerable.Contains(cmd.Description, ']'))
                {
                    var subgroup = cmd.Description.Substring(1);
                    subgroup = subgroup.Remove(subgroup.IndexOf(']'));
                    subgroups.Add((subgroup, cmd.Name));
                }
                else
                {
                    subgroups.Add(("Command(s)", cmd.Name));
                }
            }

            foreach (var sg in subgroups.Select(x => x.Item1).Distinct())
            {
                Embed.AddField(
                    Command != null ? $"{sg} (subcommands)" : $"{sg}", string.Join(", ", 
                        subgroups.Where(x => x.Item1 == sg).Select(x => Formatter.InlineCode(x.Item2))));
            }

            return this;
        }

        public override CommandHelpMessage Build()
        {
            if (Command == null)
                Embed.Description += $"{Environment.NewLine}Listing all top-level commands and groups" +
                                    $"{Environment.NewLine}You can specify a command to see more information";
            
            return new CommandHelpMessage(embed: Embed.Build());
        }
    }
}
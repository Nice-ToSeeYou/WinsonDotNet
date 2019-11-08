using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace WinsonDotNet.Modules
{
    public class Fun
    {
        [Command("makebig")]
        [Description("Converts text to emojis")]
        public async Task BigAsync(CommandContext ctx,
            [Description("Text you want to remplace"), RemainingText] string input)
        {
            var replace = _allLetters.Replace(input, "");
            await ctx.RespondAsync(string.Join(" ", replace.Select(x => $":regional_indicator_{char.ToLower(x)}:")));
        }
        
        [Command("makebeauty")]
        [Description("Make a text look beautiful")]
        public async Task EmbedTextAsync(CommandContext ctx,
            [Description("Text you want to remplace"), RemainingText] string input)
            => await ctx.RespondAsync(MakeBeauty(input));
        
        #region Beauty

        private readonly Regex _allLetters = new Regex("[^A-Z^a-z]");
        
        private readonly IReadOnlyDictionary<char, string> _doubleStruck = new Dictionary<char, string>()
        {
            {'a', "𝔸"}, {'b', "𝔹"}, {'c', "ℂ"}, {'d', "𝔻"}, {'e', "𝔼"}, {'f', "𝔽"}, {'g', "𝔾"},
            {'h', "ℍ"}, {'i', "𝕀"}, {'j', "𝕁"}, {'k', "𝕂"}, {'l', "𝕃"}, {'m', "𝕄"}, {'n', "ℕ"},
            {'o', "𝕆"}, {'p', "ℙ"}, {'q', "ℚ"}, {'r', "ℝ"}, {'s', "𝕊"}, {'t', "𝕋"}, {'u', "𝕌"},
            {'v', "𝕍"}, {'w', "𝕎"}, {'x', "𝕏"}, {'y', "𝕐"}, {'z', "ℤ"}
        };

        private string MakeDouble(string input)
        {
            var answer = string.Empty;
            foreach (var c in input)
                if (_doubleStruck.ContainsKey(char.ToLower(c))) answer += _doubleStruck[char.ToLower(c)];
                else answer += c;

            return answer;
        }

        private string MakeBeauty(string input)
        {
            return $"◦•●◉✿ {MakeDouble(input)} ✿◉●•◦";
        }

        #endregion
    }
}
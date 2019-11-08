using Newtonsoft.Json;

namespace WinsonDotNet.Main
{
    public class Configuration
    {
        static Configuration()
        {
            Default = new Configuration()
            {
                //Token
                DiscordToken = "[Insert token here]",
            
                //Bot properties
                BotName = "Winson",
                CaseSensitive = false,
                DmHelp = false,
                EnableDefaultHelp = true,
                EnableDms = false,
                DiscordPrefix = "w.",
                EnableMentionPrefix = true, 
                IgnoreExtraArguments = true,
            
                //Client properties
                AutoReconnect = true,
                DateTimeFormat = "dd/MM/yyyy HH:mm:ss",
                HttpTimeout = 60,
                LargeThreshold = 250,
                MessageCacheSize = 1000,
                UseInternalLogHandler = false,
                
                //Interaction properties
                Timeout = 5,
                
                //Others
                GitHubLink = null,
                GitHubGuidelines = null
            };
        }

        //Token
        [JsonProperty("DiscordToken")]
        public string DiscordToken { get; private set; }
        
        //Bot properties
        [JsonProperty("BotName")]
        public string BotName { get; private set; }
        
        [JsonProperty("CaseSensitive")]
        public bool CaseSensitive { get; private set; }
        
        [JsonProperty("DmHelp")]
        public bool DmHelp { get; private set; }
        
        [JsonProperty("EnableDefaultHelp")]
        public bool EnableDefaultHelp { get; private set; }
        
        [JsonProperty("EnableDms")]
        public bool EnableDms { get; private set; }
        
        [JsonProperty("EnableMentionPrefix")]
        public bool EnableMentionPrefix { get; private set; }
        
        [JsonProperty("DiscordPrefix")]
        public string DiscordPrefix { get; private set; }
        
        [JsonProperty("IgnoreExtraArguments")]
        public bool IgnoreExtraArguments { get; private set; }
        
        //Client properties
        [JsonProperty("AutoReconnect")]
        public bool AutoReconnect { get; private set; }
        
        [JsonProperty("DateTimeFormat")]
        public string DateTimeFormat { get; private set; }
        
        [JsonProperty("HttpTimeout")]
        public int HttpTimeout { get; private set; }
        
        [JsonProperty("LargeThreshold")]
        public int LargeThreshold { get; private set; }
        
        [JsonProperty("MessageCacheSize")]
        public int MessageCacheSize { get; private set; }
        
        [JsonProperty("UseInternalLogHandler")]
        public bool UseInternalLogHandler { get; private set; }
        
        //Interactivity properties
        [JsonProperty("Timeout")]
        public int Timeout { get; private set; }
        
        //Others
        [JsonProperty("GitHubLink")]
        public string GitHubLink { get; private set; }
        
        [JsonProperty("GitHubGuidelines")]
        public string GitHubGuidelines { get; private set; }
        
        [JsonIgnore]
        public static Configuration Default { get; }
    }
}
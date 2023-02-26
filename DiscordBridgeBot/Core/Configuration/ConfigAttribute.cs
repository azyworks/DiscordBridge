using AzyWorks.Configuration;

namespace DiscordBridgeBot.Core.Configuration
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ConfigAttribute : Attribute, IConfigAttribute
    {
        private string Name;
        private string Description;

        public ConfigAttribute(string name, string description = null)
        {
            Name = name;
            Description = description;
        }

        public string GetDescription() => Description;
        public string GetName() => Name;
    }
}

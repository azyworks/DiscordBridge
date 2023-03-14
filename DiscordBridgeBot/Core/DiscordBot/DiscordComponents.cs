using Discord;
using Discord.WebSocket;
using DiscordBridgeBot.Core.ScpSl;

namespace DiscordBridgeBot.Core.DiscordBot
{
    public class DiscordComponents
    {
        private DiscordService _discordService;
        private Dictionary<string, Action<SocketMessageComponent>> _buttonCallbacks = new Dictionary<string, Action<SocketMessageComponent>>();
        private Dictionary<string, Action<SocketMessageComponent>> _menuCallbacks = new Dictionary<string, Action<SocketMessageComponent>>();

        public int Port { get => (_discordService.Collection as ScpSlServer).ServerPort; }

        public void AddHandlers(DiscordService discordService)
        {
            _discordService = discordService;

            _discordService.Client.ButtonExecuted += OnButtonInteraction;
            _discordService.Client.SelectMenuExecuted += OnMenuInteraction;
        }

        public void RemoveHandlers()
        {
            _buttonCallbacks.Clear();
            _menuCallbacks.Clear();

            _discordService.Client.ButtonExecuted -= OnButtonInteraction;
            _discordService.Client.SelectMenuExecuted -= OnMenuInteraction;

            _discordService = null;
        }

        public ComponentBuilder AddButton(ComponentBuilder builder, ButtonBuilder button, Action<SocketMessageComponent> callback)
        {
            _buttonCallbacks[button.CustomId] = callback;
            return builder.WithButton(button);
        }

        public ComponentBuilder AddButton(ComponentBuilder builder, string label, string id, ButtonStyle style, Action<SocketMessageComponent> callback)
        {
            return AddButton(builder, new ButtonBuilder()
                .WithCustomId(id)
                .WithLabel(label)
                .WithStyle(style), callback);
        }

        public ComponentBuilder AddMenu(ComponentBuilder builder, SelectMenuBuilder menu, Action<SocketMessageComponent> callback)
        {
            _menuCallbacks[menu.CustomId] = callback;
            return builder.WithSelectMenu(menu);
        }

        public ComponentBuilder AddMenu(ComponentBuilder builder, string id, string placeholder, Action<SocketMessageComponent> callback, params SelectMenuOptionBuilder[] options)
        {
            return AddMenu(builder, new SelectMenuBuilder()
                .WithCustomId(id)
                .WithDisabled(false)
                .WithOptions(options.ToList()), callback);
        }

        public string GetCustomId(string customId, string serviceName, string elementType = "Button")
        {
            if (elementType != "Button" && elementType != "Menu")
                return null;

            return $"Port={Port}+{elementType}+{serviceName}+{customId}";
        }

        private async Task OnMenuInteraction(SocketMessageComponent component)
        {
            await component.DeferAsync();

            if (_menuCallbacks.TryGetValue(component.Data.CustomId, out var callback))
            {
                callback?.Invoke(component);
            }
        }

        private async Task OnButtonInteraction(SocketMessageComponent component)
        {
            await component.DeferAsync();

            if (_buttonCallbacks.TryGetValue(component.Data.CustomId, out var callback))
            {
                callback?.Invoke(component);
            }
        }
    }
}
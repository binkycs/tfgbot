using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace tfgbot
{
    internal class MatchList
    {
        private static readonly List<string> MatchUsers = new List<string>();
        static RestUserMessage _listMessage;
        static SocketGuild _guild;

        public static void Initialize(SocketGuild guild)
        {
            _guild = guild;
        }

        public static void AddToList(SocketGuildUser user)
        {
            string username = user.Nickname ?? user.Username;
            if (MatchUsers.Count == 10)
                return;
            if (MatchUsers.Contains(username))
                return;

            MatchUsers.Add(username);
        }

        public static void RemoveFromList(SocketGuildUser user)
        {
            string username = user.Nickname ?? user.Username;
            MatchUsers.Remove(username);
        }

        public static async void SendList()
        {
            //get 10 man status channel
            var channel = _guild.GetTextChannel(Program.TenManStatusId);

            var embedBuilder = new EmbedBuilder();

            embedBuilder.WithColor(new Color(7506394));
            embedBuilder.WithTitle("Current 10 man list:");
            embedBuilder.WithDescription("No one yet!");

            var x = await channel.SendMessageAsync("", false, embedBuilder.Build()).ConfigureAwait(true);

            _listMessage = x;
        }

        public static Task UpdateListAsync()
        {
            var listContent = "";

            for (var i = 0; i < MatchUsers.Count; i++)
            {
                listContent += $"\n{(i + 1)}. {MatchUsers[i]}";
            }

            listContent = listContent.Trim();

            var embedBuilder = new EmbedBuilder();

            embedBuilder.WithColor(new Color(7506394));
            embedBuilder.WithTitle("Current 10 man list:");
            embedBuilder.WithDescription(listContent);

            return _listMessage.ModifyAsync(x =>
            {
                x.Embed = embedBuilder.Build();
            });
        }
    }
}

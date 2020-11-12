using System.Collections.Generic;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace tfgbot
{
    class MatchList
    {
        private static List<SocketGuildUser> _matchList = new List<SocketGuildUser>();
        static RestUserMessage _listMessage;
        static SocketGuild _guild;

        internal static ulong ListMessageId
        {
            get { return _listMessage == null ? 0 : _listMessage.Id; }
        }

        public static void Initialize(SocketGuild guild)
        {
            _guild = guild;
        }

        //
        // Summary:
        //     Returns true if list is full after adding user
        public static void AddToList(SocketGuildUser user)
        {
            if (_matchList.Count == 10)
                return;
            if (_matchList.Contains(user))
                return;

            _matchList.Add(user);
        }
        public static void RemoveFromList(SocketGuildUser user)
        {
            _matchList.Remove(user);
        }

        public static async void SendList()
        {
            //get 10 man status channel
            var channel = _guild.GetTextChannel(Program._tenManStatusId);

            var embedBuilder = new EmbedBuilder();

            embedBuilder.WithColor(new Color(7506394));
            embedBuilder.WithTitle("Current 10 man list:");
            embedBuilder.WithDescription("No one yet!");

            var x = await channel.SendMessageAsync("", false, embedBuilder.Build());

            _listMessage = x;
        }

        public static async void UpdateList()
        {
            //string listContent = "```Current 10 Man List:";

            string listContent = "";

            for (int i = 0; i < _matchList.Count; i++)
            {
                string name = _matchList[i].Nickname ?? _matchList[i].Username;
                listContent += "\n" + (i + 1) + ". " + name;
            }

            listContent = listContent.Trim();

            var embedBuilder = new EmbedBuilder();

            embedBuilder.WithColor(new Color(7506394));
            embedBuilder.WithTitle("Current 10 man list:");
            embedBuilder.WithDescription(listContent);

            Embed embed = embedBuilder.Build();


            await _listMessage.ModifyAsync(x =>
            {
                x.Embed = embed;
            });
        }
    }
}

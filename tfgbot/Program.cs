using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MongoDB.Driver;

namespace tfgbot
{
    internal class Program
    {
        internal const string TokenLocation = "token.txt";
        internal const string ConnectionStringLocation = "database.txt";

        //server ID
        internal const ulong GuildId = 414212469771337738;

        //channel IDs
        internal const ulong TenManStatusId = 541710653857988658;
        internal const ulong TenManChatId = 497237548221988864;

        //role IDs
        internal const ulong VisitorRoleId = 617076228481875980;
        internal const ulong TenManRoleId = 616761979322892291;


        private static DiscordSocketClient _discordClient;
        private static SocketGuild _guild;

        private static MongoClient _mongoClient;

        private static ulong _lastBotTenManMessageId;

        public Program()
        {
            _discordClient = new DiscordSocketClient();

            _mongoClient = new MongoClient(File.ReadAllText(ConnectionStringLocation));

            _mongoClient = new MongoClient(File.ReadAllText(ConnectionStringLocation));
            new Task(() => { });

            //_client.Log += LogAsync;
            //_client.Ready += ReadyAsync;
            _discordClient.MessageReceived += MessageReceivedAsync;
            _discordClient.ReactionAdded += ReactionAddedAsync;
            _discordClient.ReactionRemoved += ReactionRemovedAsync;
            _discordClient.UserJoined += UserJoinedAsync;
            _discordClient.Connected += ClientConnectedAsync;
        }

        private static void Main()
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            await _discordClient.LoginAsync(TokenType.Bot, File.ReadAllText(TokenLocation)).ConfigureAwait(true);

            await _discordClient.StartAsync().ConfigureAwait(true);

            // Block the program until it is closed.
            while (true)
                if (Console.ReadLine() == "exit")
                    break;

            _discordClient.Dispose();
        }

        private static Task ClientConnectedAsync()
        {
            return Task.Run(() =>
            {
                _guild = _discordClient.GetGuild(GuildId);

                MatchList.Initialize(_guild);
                RoleAssigner.Initialize(_guild);
            });
        }

        private static async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Content != "!10man") return;

            if (message.Channel.Id == TenManChatId)
            {
                var tenManRole = _guild.GetRole(TenManRoleId);

#if DEBUG
                var botMessage = await message.Channel
                    .SendMessageAsync("Setting up a 10 manner!\nCheck the 10 Man Status Channel for the list! React to this message in order to be added to the list!")
                    .ConfigureAwait(true);
#else
                var botMessage = await message.Channel
                    .SendMessageAsync(
                        $"Setting up a 10 manner!\n{tenManRole.Mention}\ncheck the 10 Man Status Channel for the list! React to this message in order to be added to the list!")
                    .ConfigureAwait(true);
#endif

                _lastBotTenManMessageId = botMessage.Id;

                MatchList.SendList();
            }
        }

        private static Task UserJoinedAsync(SocketGuildUser user)
        {
            return RoleAssigner.AssignRoleAsync(user.Id, VisitorRoleId);
        }

        private static async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            var dbRole = FindRoleFromMessageReaction(reaction);

            if (dbRole == null)
            {
                // ReSharper disable once InvertIf
                if (reaction.MessageId == _lastBotTenManMessageId)
                {
                    var user = _guild.GetUser(reaction.UserId);
                    UpdateMatchList(user, true);
                }

                return;
            }

            if (reaction.Emote.Name == dbRole.Emoji)
            {
                if (!string.IsNullOrEmpty(dbRole.RoleId))
                    await RoleAssigner.AssignRoleAsync(reaction.UserId, ulong.Parse(dbRole.RoleId)).ConfigureAwait(true);
                if (!string.IsNullOrEmpty(dbRole.RemovalRoleId))
                    await RoleAssigner.RemoveRoleAsync(reaction.UserId, ulong.Parse(dbRole.RemovalRoleId))
                        .ConfigureAwait(true);
            }
        }

        private static async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            var dbRole = FindRoleFromMessageReaction(reaction);
            if (dbRole == null)
            {
                if (reaction.MessageId == _lastBotTenManMessageId)
                {
                    var user = _guild.GetUser(reaction.UserId);
                    UpdateMatchList(user, false);
                }

                return;
            }

            if (reaction.Emote.Name == dbRole.Emoji)
            {
                if (!string.IsNullOrEmpty(dbRole.RoleId))
                    await RoleAssigner.RemoveRoleAsync(reaction.UserId, ulong.Parse(dbRole.RoleId)).ConfigureAwait(true);
                if (!string.IsNullOrEmpty(dbRole.RemovalRoleId))
                    await RoleAssigner.AssignRoleAsync(reaction.UserId, ulong.Parse(dbRole.RemovalRoleId))
                        .ConfigureAwait(true);
            }
        }

        private static void UpdateMatchList(SocketGuildUser user, bool addUser)
        {
            if (addUser)
                MatchList.AddToList(user);
            else
                MatchList.RemoveFromList(user);

            MatchList.UpdateListAsync().ConfigureAwait(true);
        }

        private static Role FindRoleFromMessageReaction(SocketReaction reaction)
        {
            var roles = _mongoClient.GetDatabase("tfgbot").GetCollection<Role>("roles");
            return roles.Find(y => y.MessageId == reaction.MessageId.ToString()).FirstOrDefault();
        }
    }
}
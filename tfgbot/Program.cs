using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using MongoDB.Driver;
using System;
using MongoDB.Bson;
using System.Runtime.CompilerServices;

namespace tfgbot
{
    //TODO: Clean up 10 man setup

    class Program
    {
        internal const string _tokenLocation = "token.txt";
        internal const string _connectionStringLocation = "database.txt";

        //server ID
        internal const ulong _guildId = 414212469771337738;

        //channel IDs
        internal const ulong _tenManStatusId = 541710653857988658;
        internal const ulong _tenManChatId = 497237548221988864;

        //role IDs
        internal const ulong _visitorRoleId = 617076228481875980;
        internal const ulong _tenManRoleId = 616761979322892291;


        static DiscordSocketClient _discordClient;
        static SocketGuild _guild;

        static MongoClient _mongoClient;

        static ulong _lastBotTenManMessageId;

        static void Main()
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public Program()
        {
            _discordClient = new DiscordSocketClient();

            _mongoClient = new MongoClient(File.ReadAllText(_connectionStringLocation));

            //_client.Log += LogAsync;
            //_client.Ready += ReadyAsync;
            _discordClient.MessageReceived += MessageReceivedAsync;
            _discordClient.ReactionAdded += ReactionAddedAsync;
            _discordClient.ReactionRemoved += ReactionRemovedAsync;
            _discordClient.UserJoined += UserJoinedAsync;
            _discordClient.Connected += ClientConnected;
        }

        public async Task MainAsync()
        {
            await _discordClient.LoginAsync(TokenType.Bot, File.ReadAllText(_tokenLocation));

            await _discordClient.StartAsync();

            // Block the program until it is closed.
            await Task.Delay(Timeout.Infinite);
        }

        private async Task ClientConnected()
        {
            await Task.Run(() =>
            {
                _guild = _discordClient.GetGuild(_guildId);

                MatchList.Initialize(_guild);
                RoleAssigner.Initialize(_guild);
            });
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Content == "!10man" && message.Channel.Id == _tenManChatId)
            {
                var tenManRole = _guild.GetRole(_tenManRoleId);

#if DEBUG
                var botMessage = await message.Channel.SendMessageAsync("Setting up a 10 manner!\n" + /*tenManRole.Mention +*/ "\ncheck the 10 Man Status Channel for the list! React to this message in order to be added to the list!");
#else
                var botMessage = await message.Channel.SendMessageAsync("Setting up a 10 manner!\n" + tenManRole.Mention + "\ncheck the 10 Man Status Channel for the list! React to this message in order to be added to the list!");
#endif

                _lastBotTenManMessageId = botMessage.Id;

                MatchList.SendList();
            }
        }

        private async Task UserJoinedAsync(SocketGuildUser user)
        {
            await RoleAssigner.AssignRole(user.Id, _visitorRoleId);
        }

        private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var dbRole = FindRoleFromMessageReaction(reaction);

            if (dbRole == null)
            {
                if (reaction.MessageId == _lastBotTenManMessageId)
                {
                    var user = _guild.GetUser(reaction.UserId);
                    MatchList.AddToList(user);
                    MatchList.UpdateList();
                }
                return;
            }

            if (reaction.Emote.Name == dbRole.Emoji)
            {
                if (!string.IsNullOrEmpty(dbRole.RoleID))
                    await RoleAssigner.AssignRole(reaction.UserId, ulong.Parse(dbRole.RoleID));
                if (!string.IsNullOrEmpty(dbRole.RemovalRoleID))
                    await RoleAssigner.RemoveRole(reaction.UserId, ulong.Parse(dbRole.RemovalRoleID));
            }
        }

        private async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var dbRole = FindRoleFromMessageReaction(reaction);

            if (dbRole == null)
            {
                if (reaction.MessageId == _lastBotTenManMessageId)
                {
                    var user = _guild.GetUser(reaction.UserId);
                    MatchList.RemoveFromList(user);
                    MatchList.UpdateList();
                }
                return;
            }

            if (reaction.Emote.Name == dbRole.Emoji)
            {
                if (!string.IsNullOrEmpty(dbRole.RoleID))
                    await RoleAssigner.RemoveRole(reaction.UserId, ulong.Parse(dbRole.RoleID));
                if (!string.IsNullOrEmpty(dbRole.RemovalRoleID))
                    await RoleAssigner.AssignRole(reaction.UserId, ulong.Parse(dbRole.RemovalRoleID));
            }
        }

        private Role FindRoleFromMessageReaction(SocketReaction reaction)
        {
            var roles = _mongoClient.GetDatabase("tfgbot").GetCollection<Role>("roles");
            return roles.Find(y => y.MessageID == reaction.MessageId.ToString()).FirstOrDefault();
        }
    }
}

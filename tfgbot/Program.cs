using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
namespace tfgbot
{
    //TODO: Clean up 10 man setup

    class Program
    {
        internal const string _tokenLocation = "token.txt";

        //server ID
        internal const ulong _guildId = 414212469771337738;

        //channel IDs
        internal const ulong _tenManStatusId = 541710653857988658;
        internal const ulong _tenManChatId = 497237548221988864;

        //role IDs
        internal const ulong _visitorRoleId = 617076228481875980;
        internal const ulong _tenManRoleId = 616761979322892291;

        internal const ulong _valorantRoleId = 722590338014773379;
        internal const ulong _tarkovRoleId = 668574579257507841;
        internal const ulong _csgoRoleId = 776163796342931466;
        internal const ulong _amongUsRoleId = 758866844253421608;
        internal const ulong _codRoleId = 776164864216530954;

        //Array of (RoleID, Corresponding Emote Name) for self-assigning roles
        internal KeyValuePair<ulong, string>[] _gameRoleList = new KeyValuePair<ulong, string>[]
        {
            new KeyValuePair<ulong, string>(776164864216530954, "cod"),
            new KeyValuePair<ulong, string>(722590338014773379, "valorant"),
            new KeyValuePair<ulong, string>(668574579257507841, "tarkov"),
            new KeyValuePair<ulong, string>(776163796342931466, "csgo"),
            new KeyValuePair<ulong, string>(758866844253421608, "sus")
        };

        //Array of (RoleID, Corresponding Emote Name) for self-assigning rank roles
        internal KeyValuePair<ulong, string>[] _rankRoleList = new KeyValuePair<ulong, string>[]
        {
            new KeyValuePair<ulong, string>(612783689725640734, "global"),
            new KeyValuePair<ulong, string>(547218322534432770, "intermediate"),
            new KeyValuePair<ulong, string>(547218262622994442, "silver"),
            new KeyValuePair<ulong, string>(776203599340240897, "wood")
        };

        //Array of (RoleID, Corresponding Emote Name) for self-assigning specialized roles (tech support/health advisor)
        internal KeyValuePair<ulong, string>[] _specializedRoleList = new KeyValuePair<ulong, string>[]
        {
            new KeyValuePair<ulong, string>(776216721480220702, "💻"),
            new KeyValuePair<ulong, string>(776216774500286464, "💪")
        };

        //Constant Message IDs
        internal const ulong _rankAssignmentMessageId = 776128879625764884;
        internal const ulong _gameAssignmentMessageId = 776155071234048030;
        internal const ulong _specialAssignmentMessageId = 776217030285852722;


        static DiscordSocketClient _client;
        static SocketGuild _guild;

        static ulong _lastBotTenManMessageId;

        static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public Program()
        {
            _client = new DiscordSocketClient();

            //_client.Log += LogAsync;
            //_client.Ready += ReadyAsync;
            _client.MessageReceived += MessageReceivedAsync;
            _client.ReactionAdded += ReactionAddedAsync;
            _client.ReactionRemoved += ReactionRemovedAsync;
            _client.UserJoined += UserJoinedAsync;
            _client.Connected += ClientConnected;
        }

        public async Task MainAsync()
        {
            await _client.LoginAsync(TokenType.Bot, File.ReadAllText(_tokenLocation));

            await _client.StartAsync();

            // Block the program until it is closed.
            await Task.Delay(Timeout.Infinite);
        }

        private async Task ClientConnected()
        {
            await Task.Run(() =>
            {
                _guild = _client.GetGuild(_guildId);

                MatchList.Initialize(_guild);
                RoleAssigner.Initialize(_guild);
            });
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Content == "!10man" && message.Channel.Id == 497237548221988864)
            {
                var tenManRole = _guild.GetRole(_tenManRoleId);

                var botMessage = await message.Channel.SendMessageAsync("Setting up a 10 manner!\n" + tenManRole.Mention + "\ncheck the 10 Man Status Channel for the list! React to this message in order to be added to the list!");

                _lastBotTenManMessageId = botMessage.Id;

                MatchList.SendList();
            }
        }

        private async Task UserJoinedAsync(SocketGuildUser user)
        {
            await RoleAssigner.AssignRole(user.Id, _visitorRoleId);

            AuditLog.AddLog("Added visitor role to " + user.Username);
        }

        private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            //add 10 man tag
            if (channel.Id == 547217573461360652)
            {
                await RoleAssigner.AssignRole(reaction.UserId, _tenManRoleId);
            }
            //visitor removal/member role addition
            else if (channel.Id == 545752468407975946)
            {
                if (reaction.Emote.Name == "👍")
                    await RoleAssigner.RemoveRole(reaction.UserId, _visitorRoleId);
            }
            //ah it's for the 10man! :)
            else if (reaction.MessageId == _lastBotTenManMessageId)
            {
                var user = _guild.GetUser(reaction.UserId);
                MatchList.AddToList(user);
                MatchList.UpdateList();
            }
            //rank/skill level assignment
            else if (reaction.MessageId == _rankAssignmentMessageId)
            {
                var rolePair = FindRole(_rankRoleList, reaction.Emote.Name);
                if (rolePair.Key == 0)
                    return;
                await RoleAssigner.AssignRole(reaction.UserId, rolePair.Key);
            }
            //user is adding a game role to their profile :)
            else if (reaction.MessageId == _gameAssignmentMessageId)
            {
                var rolePair = FindRole(_gameRoleList, reaction.Emote.Name);
                if (rolePair.Key == 0)
                    return;
                await RoleAssigner.AssignRole(reaction.UserId, rolePair.Key);
            }
            //user is adding a specialized role (tech support/health advisor)
            else if (reaction.MessageId == _specialAssignmentMessageId)
            {
                var rolePair = FindRole(_specializedRoleList, reaction.Emote.Name);
                if (rolePair.Key == 0)
                    return;

                await RoleAssigner.AssignRole(reaction.UserId, rolePair.Key);
            }
        }

        private async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            //add 10 man tag
            if (channel.Id == 547217573461360652)
            {
                await RoleAssigner.RemoveRole(reaction.UserId, _tenManRoleId);
            }
            //check if rules updoot got removed, give visitor role again
            else if (channel.Id == 545752468407975946)
            {
                if (reaction.Emote.Name != "👍")
                    return;

                await RoleAssigner.AssignRole(reaction.UserId, _visitorRoleId);
                await RoleAssigner.RemoveRole(reaction.UserId, _tenManRoleId);
            }
            //someone's backing out of 10 man :(
            else if (channel.Id == _tenManChatId && reaction.MessageId == _lastBotTenManMessageId)
            {
                var user = _guild.GetUser(reaction.UserId);

                MatchList.RemoveFromList(user);
                MatchList.UpdateList();
            }
            //rank/skill level removal
            else if (reaction.MessageId == _rankAssignmentMessageId)
            {
                var rolePair = FindRole(_rankRoleList, reaction.Emote.Name);
                if (rolePair.Key == 0)
                    return;

                await RoleAssigner.RemoveRole(reaction.UserId, rolePair.Key);
            }
            //user is removing a game role from their profile :(
            else if (reaction.MessageId == _gameAssignmentMessageId)
            {
                var rolePair = FindRole(_gameRoleList, reaction.Emote.Name);
                if (rolePair.Key == 0)
                    return;

                await RoleAssigner.RemoveRole(reaction.UserId, rolePair.Key);
            }
            //user is adding a specialized role (tech support/health advisor)
            else if (reaction.MessageId == _specialAssignmentMessageId)
            {
                var rolePair = FindRole(_specializedRoleList, reaction.Emote.Name);
                if (rolePair.Key == 0)
                    return;

                await RoleAssigner.RemoveRole(reaction.UserId, rolePair.Key);
            }
        }

        private KeyValuePair<ulong, string> FindRole(KeyValuePair<ulong, string>[] list, string emojiName)
        {
            foreach (KeyValuePair<ulong, string> x in list)
                if (x.Value == emojiName)
                    return x;

            return new KeyValuePair<ulong, string>(0, null);
        }
    }
}

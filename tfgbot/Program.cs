using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using MongoDB.Bson;
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
        internal const ulong ServerRulesId = 545752468407975946;
        internal const ulong TenManRulesId = 547217573461360652;
        internal const ulong TenManStatusId = 541710653857988658;
        internal const ulong TenManChatId = 497237548221988864;

        //rule message ids
        internal const ulong ServerRulesMessageId = 613425588425850917;
        internal const ulong TenManRulesMessageId = 615686381586612239;

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
            {
                var line = Console.ReadLine();
                //user wants to exit
                if (line == "exit")
                    break;
                //Recheck 
                //TODO: Clean this up, its so ugly
                if (line == "recheck")
                {
                    var serverNonReacts = await GetNonReactUsersAsync(ServerRulesId, ServerRulesMessageId).ConfigureAwait(true);
                    //TODO: My mind is burnt out right now, i was gonna do something with this earlier
                    //var tenManNonReacts = await GetNonReactUsers(TenManRulesId, TenManRulesMessageId).ConfigureAwait(true);
                    var visitorRole = GetSocketRoleFromId(VisitorRoleId);

                    foreach (var x in serverNonReacts)
                    {
                        if (x.IsBot) continue;

                        var fUser = _guild.Users.First(y => y.Id == x.Id);

                        if (fUser.Roles.Count == 2 && fUser.Roles.Contains(visitorRole)) continue;
                        var guildUser = _guild.GetUser(fUser.Id);

                        foreach (var r in guildUser.Roles)
                        {
                            if (r.IsEveryone) continue;
                            await RoleAssigner.RemoveRoleAsync(guildUser, r).ConfigureAwait(true);
                        }

                        await RoleAssigner.AssignRoleAsync(x.Id, VisitorRoleId).ConfigureAwait(true);
                    }

                }
            }

            _discordClient.Dispose();
        }

        private static async Task<SocketUser[]> GetNonReactUsersAsync(ulong channelId, ulong messageId)
        {
            var users = _guild.Users.ToArray();
            var channel = _guild.GetTextChannel(channelId);

            var thumbsUp = new Emoji("\U0001F44D");
            var message = await channel.GetMessageAsync(messageId).ConfigureAwait(true);

            var reactions = await message.GetReactionUsersAsync(thumbsUp, int.MaxValue).ToArrayAsync().ConfigureAwait(true);

            var reactedUsers = reactions.SelectMany(x => x.ToArray()).ToList();

            var nonReactUsers = new List<SocketUser>();

            foreach (var user in users)
            {
                var exists = reactedUsers.Any(x => x.Id == user.Id);

                if (!exists)
                    nonReactUsers.Add(user);
            }

            return nonReactUsers.ToArray();
        }

        private static Task ClientConnectedAsync()
        {
            return Task.Run(() =>
            {
                _guild = _discordClient.GetGuild(GuildId);

                DownloadUsers();

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
            DownloadUsers();
            return RoleAssigner.AssignRoleAsync(user.Id, VisitorRoleId);
        }

        private static async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            var dbRole = GetRoleFromReaction(reaction);

            if (dbRole == null)
            {
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
                    await RoleAssigner.AssignRoleAsync(reaction.UserId, ulong.Parse(dbRole.RoleId))
                        .ConfigureAwait(true);

                if (!string.IsNullOrEmpty(dbRole.RemovalRoleId))
                    await RoleAssigner.RemoveRoleAsync(reaction.UserId, ulong.Parse(dbRole.RemovalRoleId))
                        .ConfigureAwait(true);
            }
        }

        private static async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            var dbRole = GetRoleFromReaction(reaction);
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
                    await RoleAssigner.RemoveRoleAsync(reaction.UserId, ulong.Parse(dbRole.RoleId))
                        .ConfigureAwait(true);

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

        private static Role GetRoleFromReaction(SocketReaction reaction)
        {
            return GetRoleFromId(reaction.MessageId);
        }

        private static Role GetRoleFromId(ulong id)
        {
            var roles = _mongoClient.GetDatabase("tfgbot").GetCollection<Role>("roles");
            return roles.Find(y => y.MessageId == id.ToString()).FirstOrDefault();
        }

        private static SocketRole GetSocketRoleFromId(ulong id)
        {
            var roles = _mongoClient.GetDatabase("tfgbot").GetCollection<Role>("roles");
            return _guild.Roles.FirstOrDefault(x => x.Id == id);
        }

        private static void DownloadUsers()
        {
            _guild.DownloadUsersAsync();
        }
    }
}
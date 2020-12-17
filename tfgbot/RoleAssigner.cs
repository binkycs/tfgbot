using System.Threading.Tasks;
using Discord.WebSocket;

namespace tfgbot
{
    internal class RoleAssigner
    {
        private static SocketGuild _guild;
        public static void Initialize(SocketGuild guild)
        {
            _guild = guild;
        }
        public static async Task AssignRoleAsync(ulong user, ulong role)
        {
            var svUser = _guild.GetUser(user);
            var svRole = _guild.GetRole(role);

            await AssignRoleAsync(svUser, svRole).ConfigureAwait(true);
        }

        public static async Task AssignRoleAsync(SocketGuildUser user, SocketRole role)
        {
            await user.AddRoleAsync(role).ConfigureAwait(true);

            AuditLog.AddLog($"Assigned {role.Name} to {user.Username}#{user.Discriminator}");
        }

        public static async Task RemoveRoleAsync(ulong user, ulong role)
        {
            var svUser = _guild.GetUser(user);
            var svRole = _guild.GetRole(role);

            await RemoveRoleAsync(svUser, svRole).ConfigureAwait(true);
        }

        public static async Task RemoveRoleAsync(SocketGuildUser user, SocketRole role)
        {
            await user.RemoveRoleAsync(role).ConfigureAwait(true);

            AuditLog.AddLog($"Removed {role.Name} from {user.Username}#{user.Discriminator}");
        }
    }
}

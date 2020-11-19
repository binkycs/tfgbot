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

            await svUser.AddRoleAsync(svRole).ConfigureAwait(true);

            AuditLog.AddLog("Assigned " + svRole.Name + " to " + svUser.Username + "#" + svUser.Discriminator);
        }
        public static async Task RemoveRoleAsync(ulong user, ulong role)
        {
            var svUser = _guild.GetUser(user);
            var svRole = _guild.GetRole(role);

            await svUser.RemoveRoleAsync(svRole).ConfigureAwait(true);

            AuditLog.AddLog("Removed " + svRole.Name + " from " + svUser.Username + "#" + svUser.Discriminator);
        }
    }
}

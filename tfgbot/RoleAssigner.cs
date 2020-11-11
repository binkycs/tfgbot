using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace tfgbot
{
    class RoleAssigner
    {
        private static SocketGuild _guild;
        public static void Initialize(SocketGuild guild)
        {
            _guild = guild;
        }
        public static async Task AssignRole(ulong user, ulong role)
        {
            var svUser = _guild.GetUser(user);
            var svRole = _guild.GetRole(role);

            await svUser.AddRoleAsync(svRole);

            AuditLog.AddLog("Assigned " + svRole.Name + " to " + svUser.Username + "#" + svUser.Discriminator);
        }
        public static async Task RemoveRole(ulong user, ulong role)
        {
            var svUser = _guild.GetUser(user);
            var svRole = _guild.GetRole(role);

            await svUser.RemoveRoleAsync(svRole);

            AuditLog.AddLog("Removed " + svRole.Name + " from " + svUser.Username + "#" + svUser.Discriminator);
        }
    }
}

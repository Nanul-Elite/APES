// APES is free and open-source software licensed under AGPL-3.0. See LICENSE file for details.
using Discord.WebSocket;

namespace APES
{
    public static class GuildUtils
    {
        public static async Task LeaveGuild(ulong guildId)
        {
            SocketGuild? guild = GetGuild(guildId);
            if(guild == null) 
                return;

            await guild.LeaveAsync();
        }

        public static async Task OnJoinCheckAndLeaveIfNotApproved(SocketGuild guild)
        {
            var defaultChannel = guild.SystemChannel;
            var approvedGuilds = Program.Config.approvedGuilds;
            Console.WriteLine($"### APES: Someone is trying to cage me");

            if (Program.Config.useWhitelist && !approvedGuilds.Contains(guild.Id))
            {
                if(defaultChannel != null)
                    await defaultChannel.SendMessageAsync("You try to cage me ?!\nApe is angry :triumph:\nGoodbye!");

                Console.WriteLine($"### APES: Left bad guild");
                await guild.LeaveAsync();
            }
            else
            {
                if (defaultChannel != null)
                {
                    await defaultChannel.SendMessageAsync($"Ape Ape Hello !\ntype `/ape` for help");
                }

                string guildDetails = Program.Config.useWhitelist ? $": {guild.Name} ({guild.Id})" : "";

                Console.WriteLine($"### APES: Joined good guild{guildDetails}");

                if (!DatabaseServices.TryGetGuildData(guild.Id, out _))
                    await DatabaseServices.CreateGuildDataAsync(guild);
            }
        }

        public static async Task<string> GetGuildUserName(SocketGuild guild, ulong userId)
        {
            SocketGuildUser guildUser = guild.GetUser(userId);
            if (Program.Config.debug && userId > Program.FakeIdThreshold)
            {
                Data.DiscordUserData? userData = await DatabaseServices.GetOrCreateDiscordUserData(guild, userId);
                if (userData != null)
                    return userData.UserName;
                else
                    return "Not Found";
            }
            else
            {
                if (guildUser != null)
                    return $"{guildUser.Nickname ?? guildUser.DisplayName}";
                else
                    return "Not Found";
            }
        }

        public static SocketGuild? GetGuild(ulong guildId)
        {
            return Program.Client.Guilds.Where(g => g.Id == guildId).FirstOrDefault();
        }
    }
}
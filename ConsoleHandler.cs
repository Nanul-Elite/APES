// APES is free and open-source software licensed under AGPL-3.0. See LICENSE file for details.
using Discord.WebSocket;
using static APES.Program;

namespace APES
{
    public class ConsoleHandler
    {
        public async Task ListenToConsole(DiscordSocketClient client)
        {
            while (true)
            {
                var input = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(input))
                    continue;

                switch (input.ToLower())
                {
                    case "ape reload config":
                        Console.WriteLine("### APES: Yes Master...");
                        I.LoadConfig();

                        if (Config == null || string.IsNullOrEmpty(Config.token))
                            Console.Error.WriteLine("### APES: I have failed you Master");
                        else
                            Console.WriteLine("### APES: Config Reloaded");

                        break;
                    case string s when s.StartsWith("ape leave guild "):
                        await LeaveGuildAsync(s);
                        break;
                    case "ape?":
                        Console.WriteLine("ape reload config - to reload config");
                        Console.WriteLine("ape leave guild [guild id] - leave a guild by id");
                        Console.WriteLine("ape post patch notes to all - send patch notes to all guilds");
                        Console.WriteLine("ape post patch notes at [guild id] : [channel name] - send patch notes to a guild in a channel");
                        break;
                    case string s when s.StartsWith("ape post patch notes to all"):
                        PostPatchNotesToAll(client);
                        break;
                    case string s when s.StartsWith("ape post patch notes at"):
                        PostPatchNotesTo(s);
                        break;
                }
            }
        }

        private async Task LeaveGuildAsync(string command)
        {
            var idString = command.Substring("ape leave guild ".Length);
            ulong guildId = ulong.Parse(idString);
            await GuildUtils.LeaveGuild(guildId);
        }

        private void PostPatchNotesToAll(DiscordSocketClient client)
        {
            foreach (var guild in client.Guilds)
            {
                PostPatchNotes(guild);
            }
        }

        private void PostPatchNotesTo(string command)
        {
            var split = command.Substring("ape post patch notes at ".Length).Split(':');
            if (split.Length < 2)
            {
                Console.WriteLine("Usage: ape post patch notes at [guild id] : [channel name]");
                return;
            }

            ulong guildId = ulong.Parse(split[0].Trim());
            string channelName = string.Join(":", split.Skip(1)).Trim();

            var guild = GuildUtils.GetGuild(guildId);
            if (guild == null)
            {
                Console.WriteLine($"Guild not found.");
                return;
            }

            var channel = guild.TextChannels.FirstOrDefault(c => c.Name == channelName);
            if (channel == null)
            {
                Console.WriteLine($"Did not find channel");
                return;
            }

            string combined = string.Join("\n", Config.patchNotes);
            channel.SendMessageAsync(combined);

            Console.WriteLine($"### Patch Notes Posted To {guild.Name} - {channel.Name}");
        }

        private void PostPatchNotes(SocketGuild guild)
        {
            var channel = GetFirstWritableChannel(guild);
            if (channel == null) return;

            string combined = string.Join("\n", Config.patchNotes);
            channel.SendMessageAsync(combined);
        }

        private SocketTextChannel? GetFirstWritableChannel(SocketGuild guild)
        {
            var botUser = guild.CurrentUser;

            if(botUser == null) return null;

            var writableChannels = guild.TextChannels
                .Where(c =>
                {
                    var perms = botUser.GetPermissions(c);
                    return perms.ViewChannel && perms.SendMessages;
                })
                .OrderBy(c => c.Position);

            return writableChannels.OrderBy(c =>
                {
                    if (c.Name.Contains("match", StringComparison.OrdinalIgnoreCase)) return 0;
                    if (c.Name.Contains("arena", StringComparison.OrdinalIgnoreCase)) return 1;
                    if (c.Name.Contains("bot", StringComparison.OrdinalIgnoreCase)) return 2;
                    return 3;
                })
                .FirstOrDefault();
        }
    }
}

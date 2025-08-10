// APES is free and open-source software licensed under AGPL-3.0. See LICENSE file for details.
using APES.Data;
using Discord;
using Discord.WebSocket;

namespace APES
{
    public class EmbedFactory
    {
        public static async Task<Embed> BuildMatchEmbed(MatchInstance match)
        {
            var embedBuilder = new EmbedBuilder();
            int[] matchConfig = match.MatchType;

            SocketGuild? guild = (match.Channel as SocketGuildChannel)?.Guild;
            if (guild == null) return embedBuilder.WithTitle("Something Went Wrong").Build();

            string limit = "";
            if (match.limitPlayerCount)
                limit = $"[{match.PlayerList.Count}/{matchConfig.Sum()}]";
            else
                limit = $"[{match.PlayerList.Count}/∞]";

            // Title with match type if provided
            string title = "";
            if (matchConfig.Length == 2)
                title += $"{matchConfig[0]}v{matchConfig[1]}  -  {limit}";
            else if (matchConfig.Length == 1)
                title += $"{(match.preTeams ? "Predefined" : "")} {matchConfig[0]} Teams  -  {limit}";

            if (match.split)
                title += "   (split)";

            embedBuilder.WithTitle(title);

            var unix = ((DateTimeOffset)match.TimeoutAt).ToUnixTimeSeconds();
            embedBuilder.Description = $"Brawl will expire <t:{unix}:R>";

            if(match.state == MatchState.open && match.preTeams == false)
            {
                embedBuilder.WithColor(Color.Blue);
                // Numbered list of all players
                var lines = await Task.WhenAll(match.PlayerList.Select(async (userId, i) => $"{i + 1}. {await GuildUtils.GetGuildUserName(guild, userId)}"));

                string playerList = lines.Length > 0
                    ? string.Join("\n", lines)
                    : "*No apes joined yet.*";

                embedBuilder.AddField("Enrolled Apes:", playerList, false);
                
                if(match.PlayerList.Count == 0)
                    embedBuilder.WithFooter("Type ape? for help");
            }

            if (match.Teams != null && match.Teams.Count > 0) // If match has rolled teams
            {
                if (match.state == MatchState.rolled)
                    embedBuilder.WithColor(Color.Green);
                else if (match.state == MatchState.locked)
                    embedBuilder.WithColor(Color.Red);
                else if (match.state == MatchState.waitingForResults)
                    embedBuilder.WithColor(Color.Purple);

                // Show teams
                for (int i = 0; i < match.Teams.Count; i++)
                {
                    var teamMembers = match.Teams[i];

                    var teamLines = await Task.WhenAll(teamMembers.Select(async (userId, j) => $"{j + 1}. { await GuildUtils.GetGuildUserName(guild, userId)}"));
                    string teamText = teamMembers.Count > 0
                        ? string.Join("\n", teamLines)
                        : "*No Apes*";

                    if (i % 2 == 1)
                    {
                        embedBuilder.AddField("vs", "\u200B", true);
                    }

                    embedBuilder.AddField($"Team {i + 1}", teamText, true);
                }

                // Check for players left out (not assigned to any team)
                var assigned = match.Teams.SelectMany(t => t).ToHashSet();
                var leftOut = match.PlayerList.Where(p => !assigned.Contains(p)).ToList();
                if (leftOut.Count > 0)
                {
                    var leftLines = await Task.WhenAll(leftOut.Select(async userId => $"- {await GuildUtils.GetGuildUserName(guild, userId)}"));
                    string leftOutText = string.Join("\n", leftLines);
                    embedBuilder.AddField("Left Out", leftOutText, false);
                }
            }


            return embedBuilder.Build();
        }

        public static Embed BuildHelpEmbed(GuildSettings guildSettings, string[] textArray)
        {
            if (textArray == null || textArray.Length == 0)
                throw new ArgumentException("Text array is null or empty.", nameof(textArray));

            string title = textArray[0]; // First line becomes the title
            string[] descriptionLines = textArray.Skip(1).ToArray();

            string commands = string.Join(", ", guildSettings.StartMatchKeywords.Select(s => $"`{guildSettings.CommandChar}{s}`"));
            string cmdChr = guildSettings.CommandChar;
            string firstCmd = guildSettings.StartMatchKeywords[0];
            string version = Program.version;

            string filledDescription = string.Join("\n", descriptionLines)
                .Replace("{commands}", commands)
                .Replace("{cmdChr}", cmdChr)
                .Replace("{firstCmd}", firstCmd)
                .Replace("{version}", version);

            var embedBuilder = new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(filledDescription);

            return embedBuilder.Build();
        }

        public static Embed BuildLeaderBoardEmbed(Dictionary<string, (int, int, int)> leaders)
        {
            var embedBuilder = new EmbedBuilder().WithTitle("Leader Board");

            string position = string.Join("\n", leaders.Select((kvp, i) => $"{i + 1} "));
            string names = string.Join("\n", leaders.Select(kvp => $"{kvp.Key}"));
            string scores = string.Join("\n", leaders.Select(kvp =>
            {
                var (rank, wins, losses) = kvp.Value;
                return $"| {rank} | {wins} | {losses}";
            }));

            embedBuilder.AddField("#", position, true);
            embedBuilder.AddField("Name", names, true);
            embedBuilder.AddField("Score | W | L", scores, true);

            return embedBuilder.Build();
        }

        public static async  Task<Embed> BuildMatchEndEmbed(List<Participant> winningTeam, List<Participant> losingTeam, int score, float expected, ulong guildId)
        {
            var embedBuilder = new EmbedBuilder().WithTitle($"Match Scores: {score}").WithDescription($"-# Expected Win Rate: {expected * 100:0}% / {(1f-expected) * 100:0}%");

            var guild = GuildUtils.GetGuild(guildId);

            var winLines = await Task.WhenAll(winningTeam.Select(async p =>
            {
                var user = DatabaseServices.TryGetDiscordUserData(p);
                if (user == null) return "- no data found";
                var name = (user.hideScore || user.optOutData) ? "Hidden" : await GuildUtils.GetGuildUserName(guild!, user.UserId);
                var rank = (user.hideScore || user.optOutData) ? "" : p.Rank.ToString();
                return $"- {name}: {rank}";
            }));
            string winnersList = string.Join("\n", winLines);

            var loseLines = await Task.WhenAll(losingTeam.Select(async p =>
            {
                var user = DatabaseServices.TryGetDiscordUserData(p);
                var name = (user.hideScore || user.optOutData) ? "Hidden" : await GuildUtils.GetGuildUserName(guild!, user.UserId);
                var rank = (user.hideScore || user.optOutData) ? "" : p.Rank.ToString();
                return $"- {name}: {rank}";
            }));
            string losersList = string.Join("\n", loseLines);

            embedBuilder.AddField("Winners:", winnersList, true);
            embedBuilder.AddField("vs", "\u200B​", true);
            embedBuilder.AddField("Losers:", losersList, true);

            return embedBuilder.Build();
        }
    }
}

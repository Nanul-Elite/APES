// APES is free and open-source software licensed under AGPL-3.0. See LICENSE file for details.
using APES.Data;
using Discord.WebSocket;

namespace APES
{
    public class RankServices
    {
        public static async Task<Dictionary<string, (int, int, int)>> GetLeaderBoardAsync(ulong guildId)
        {
            var leaderBoard = new Dictionary<string, (int, int, int)>(); // Rank, Name
            var participants = await DatabaseServices.GetTournamentParticipantsDataAsync(guildId);
            SocketGuild? guild = GuildUtils.GetGuild(guildId);

            if (guild == null || participants == null) 
                return leaderBoard;

            foreach (var participant in participants)
            {
                DiscordUserData? discordUserData = DatabaseServices.TryGetDiscordUserData(participant);
                if (discordUserData == null) continue;

                if (discordUserData != null)
                {
                    string userName = await GuildUtils.GetGuildUserName(guild, discordUserData.UserId);
                    if (!leaderBoard.ContainsKey(userName) && !discordUserData.hideScore && !discordUserData.optOutData)
                    {
                        leaderBoard.Add(userName, (participant.Rank, participant.MatchesWon, participant.MatchesLost));
                    }
                }
            }

            return leaderBoard.OrderByDescending(item => item.Value.Item1)
                             .ThenByDescending(item => item.Value.Item2 + item.Value.Item3)
                             .ThenByDescending(item => item.Value.Item2)
                             .Take(30)
                             .ToDictionary(item => item.Key, item => item.Value);
        }

        public static float CalculateTeamExpected(List<Participant> winners, List<Participant> losers)
        {
            float totalExpected = 0.0f;

            foreach (var winner in winners)
            {
                foreach (var loser in losers)
                {
                    totalExpected += GetExpectedScore(winner.Rank, loser.Rank);
                }
            }

            return totalExpected / (winners.Count * losers.Count);
        }

        private static float CalculateTeamSizeDifferenceModifier(float winners, float losers, float steepness)
        {
            float s1 = Math.Min(winners, losers);
            float s2 = Math.Max(winners, losers);

            float x = s1 / s2;
            float mod = (float)((Math.Exp(x * steepness) - 1f) / (Math.Exp(steepness) - 1f));

            if (losers < winners)
                mod = 1f - mod;

            return mod * 2;
        }

        public static (int, float) ApplyMatchResults(List<Participant> winners, List<Participant> losers, float kFactor = 40, float steepness = 6)
        {
            float totalExpected = CalculateTeamExpected(winners, losers);
            
            if(winners.Count != losers.Count)
                totalExpected *= CalculateTeamSizeDifferenceModifier(winners.Count, losers.Count, steepness);


            int score = (int)Math.Ceiling(kFactor * (1f - totalExpected));

            foreach (var winner in winners)
            {
                var userData = DatabaseServices.TryGetDiscordUserData(winner);
                if (userData == null || userData.optOutData) 
                    continue;

                winner.Rank += score;
                winner.MatchesWon++;
            }

            foreach (var loser in losers)
            {
                var userData = DatabaseServices.TryGetDiscordUserData(loser);
                if (userData == null || userData.optOutData)
                    continue;

                loser.Rank -= score;
                loser.MatchesLost++;
            }

            return (score, totalExpected);
        }

        // Expected score (win probability):
        // E = 1 / (1 + 10 ^ (-(R_opponent - R_player) / scale))

        // Rating delta:
        // delta = K * (score_actual - E)

        // Final rating:
        // R_new = R_old + delta

        public static float GetExpectedScore(float winnerRating, float loserRating, float scale = 400)
        {
            return 1.0f / (1.0f + (float)Math.Pow(10, (loserRating - winnerRating) / scale));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APES.Data;

namespace APES
{
    public enum TournamentType
    {
        Default = 0, // Server leaderboard, ELO ranking with no team limit, no predefined teams 
        Season = 1,
        SingleElimination = 2,
        DoubleElimination = 3,
    }

    public class TournamentServices
    {
        public static async Task<TournamentData> CreateTournamentData(
            int guildDataId,
            string name, 
            TournamentType type, 
            int teamSize, 
            DateTime? startDate = null,
            DateTime? endDate = null,
            DateTime? closeSignup = null,
            string description = "", 
            int minMatches = 0, 
            int maxMatches = 999, 
            float maxRankGap = 0, 
            int rankGapMatchesThreshold = 0, 
            int sameOpponentLimit = 0,
            float sameOpponenReset = 0)
        {
            var tournament = new TournamentData()
            {
                GuildDataId = guildDataId,
                Name = name,
                Description = description,
                Type = (int)type,
                TeamSize = teamSize,
                MinMatches = minMatches,
                MaxMatches = maxMatches,
                MaxRankGap = maxRankGap,
                RankGapMatchesThreshold = rankGapMatchesThreshold,
                SameOpponentLimit = sameOpponentLimit,
                SameOpponenReset = sameOpponenReset,
                Start = startDate,
                End = endDate,
                CloseSignup = closeSignup,

                Participants = new List<Participant>(),
                Teams = new List<TeamData>(),
                TournamentMatch = new List<TournamentMatch>(),
                TournamentChallanges = new List<TournamentChallange>()
            };

            Program.DB.TournamentDatas.Add(tournament);
            await Program.DB.SaveChangesAsync();

            return tournament;
        }
    }
}

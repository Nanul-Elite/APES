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

    public enum TournamentState
    {
        InSetup = 0,
        Started = 1,
        Closed = 2,
    }

    public class TournamentServices
    {
        public static TournamentData CreateTournamentData(
            string name, 
            string description,
            TournamentType type, 
            TournamentState state = TournamentState.InSetup)
        {
            var tournament = new TournamentData()
            {
                State = (int)state,
                Name = name,
                Description = description,
                Type = (int)type,

                Participants = new List<Participant>(),
                Teams = new List<TeamData>(),
                TournamentMatch = new List<TournamentMatch>(),
                TournamentChallanges = new List<TournamentChallange>()
            };

            return tournament;
        }
    }
}
/*
            int teamSize, 
            DateTime? startDate = null,
            DateTime? endDate = null,
            DateTime? closeSignup = null,
            string description = "", 
            int minMatches = 0, 
            int maxMatches = 999,
            int maxMatchesPerTimeframe = 0,
            int TimeFrameLimit = 0,
            float maxRankGap = 0, 
            int rankGapMatchesThreshold = 0, 
            int sameOpponentLimit = 0,
            float sameOpponenReset = 0,
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Net;
using Discord.WebSocket;

namespace APES.Data
{
    public class GuildData
    {
        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public string? Name { get; set; }
        public int MaxTournaments { get; set; }
        public List<TournamentData>? Tournaments { get; set; }
        public GuildSettings? GuildSettings { get; set; }
    }

    public class GuildSettings
    {
        // Data
        public int Id { get; set; }
        public string CommandChar { get; set; }
        public string[] StartMatchKeywords { get; set; }
        public List<RolePermission>? RolePermissions { get; set; }
        public bool UseReactions { get; set; }
        public List<Reaction>? BotReactions { get; set; }

        // Key
        public int GuildDataId { get; set; }
        public GuildData? GuildData { get; set; }
    }

    public class TournamentData
    {
        // Data
        public int Id { get; set; }
        public List<Participant>? Participants { get; set; }
        public List<TeamData>? Teams { get; set; } // Added
        public List<TournamentMatch>? TournamentMatch { get; set; } // Added
        public List<TournamentChallange>? TournamentChallanges { get; set; } // Added

        public string? Name { get; set; } // Added
        public string? Description { get; set; } // Added

        public int State { get; set; }
        // Single Elimination, Season etc... Should match an enum
        public int Type { get; set; } // Added
        public int TeamSize { get; set; } // Added
        /// <summary>
        /// Does this tournament requires to register loadouts, 0 = no, >0 the amount of allowed loadouts
        /// </summary>
        public int RequiredLoadouts { get; set; } // Added

        // This set of Min & Max matches are to prevent a bias twards players who have a lot of time to play a lot of matches
        // The Timeframe limit set a limit of how many matches a participant/team can have in a given time frame, the time frame resets every TimeframeLimit
        public int MinMatches { get; set; } // Added
        /// <summary>
        /// Max matches for the entire tournament per participant
        /// </summary>
        public int MaxMatches { get; set; } // Added
        /// <summary>
        /// Max matches for the timeframe, per participant
        /// </summary>
        public int MaxMatchesPerTimeframe { get; set; } // Added
        /// <summary>
        /// Timeframe in days, for the MaxTimeframeLimit
        /// </summary>
        public int TimeFrameLimit { get; set; } // Added

        // To prevent High ranking players from farming low ranking players for scores
        /// <summary>
        /// The allowed gap between 2 participants, as a percentage of the Ranks range in the tournament
        /// </summary>
        public float? MaxRankGap { get; set; } // Added
        /// <summary>
        /// After how many matches should the Gap limit start to apply
        /// </summary>
        public int? RankGapMatchesThreshold { get; set; } // Added

        // To encourage higher score spread
        /// <summary>
        /// How many times a player can fight the same opponent
        /// </summary>
        public int? SameOpponentLimit { get; set; } // Added
        /// <summary>
        /// When to rest the SameOpponentLimit, a percentage of the available opponents pool, once the threshold is reached the limit is removed
        /// </summary>
        public float? SameOpponenReset { get; set; } // Added

        public int EloMaxPointsChange { get; set; } // Added
        public int EloScale {  get; set; } // Added
        public int EloStartRank { get; set; } // Added

        public DateTime? Start {  get; set; } // Added
        public DateTime? CloseSignup { get; set; } // Added
        public DateTime? End { get; set; } // Added

        // Key
        public int GuildDataId { get; set; }
        public GuildData? GuildData { get; set; }
    }

    public class Participant
    {
        // Data
        public int Id { get; set; }
        public int Rank { get; set; }
        public int MatchesWon { get; set; }
        public int MatchesLost { get; set; }
    
        // Keys
        public int TournamentDataId { get; set; }
        public TournamentData? TournamentData { get; set; }
        public int DiscordUserDataId { get; set; }
    }

    public class TeamData // New Class Added
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<Participant>? Members { get; set; }
        public List<TournamentMatch>? TournamentMatches { get; set; }
        public List<LoadoutData>? Loadouts { get; set; }

        public int Rank { get; set; }
        public int MatchesWon { get; set; }
        public int MatchesLost { get; set; }

        public int TournamentDataId { get; set; }
        public TournamentData? TournamentData { get; set; }
    }

    public class LoadoutData // new class Added
    {
        public int Id { get; set; }
        public Participant? Participant { get; set; }
        public string? Ship {  get; set; }
        public string? Loadout {  get; set; }

        public int TeamDataId { get; set; }
        public TeamData? TeamData { get; set; }
    }

    public class TournamentMatch // New Class Added
    {
        public int Id { get; set; }
        public List<TeamData>? Teams { get; set; }
        public int State { get; set; }
        public int WinnerIdx { get; set; }
        public List<string>? VideoUrls { get; set; }

        // Keys
        public int TournamentDataId { get; set; }
        public TournamentData? TournamentData { get; set; }
    }

    public class TournamentChallange // New Class Added
    {
        public int Id { get; set; }
        public TeamData Challanger { get; set; }
        public TeamData Opponent { get; set; }

        // Keys
        public int TournamentDataId { get; set; }
        public TournamentData? TournamentData { get; set; }
    }

    public class RolePermission
    {
        // Data
        public int Id { get; set; }
        public ulong RoleId { get; set; }
        public bool CanModifyMatch { get; set; }
        public bool CanModifySettings { get; set; }

        // Key
        public int GuildSettingsId { get; set; }
        public GuildSettings? GuildSettings { get; set; }
    }

    public class Reaction
    {
        // Data
        public int Id { get; set; }

        public string[]? TriggerWords { get; set; }
        public string[]? Responses { get; set; }

        // Key
        public int GuildSettingsId { get; set; }
        public GuildSettings? GuildSettings { get; set; }
    }
}

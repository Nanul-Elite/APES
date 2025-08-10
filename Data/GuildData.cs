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
        public List<TournamentData>? Tournamens { get; set; }
        public GuildSettings? GuildSettings { get; set; }
    }

    public class GuildSettings
    {
        // Data
        public int Id { get; set; }
        public string CommandChar { get; set; }
        public string[] HelpKeywords { get; set; }
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

        // Key
        public int GuildDataId { get; set; }
        public GuildData? GuildData { get; set; }
    }

    public class Participant
    {
        // Data
        public int Id { get; set; }
        public int DiscordUserDataId { get; set; }
        public int Rank { get; set; }
        public int MatchesWon { get; set; }
        public int MatchesLost { get; set; }

        // Key
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

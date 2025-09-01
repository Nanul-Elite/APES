// APES is free and open-source software licensed under AGPL-3.0. See LICENSE file for details.
using Discord.Interactions;
using Discord.WebSocket;
using System.Text.RegularExpressions;
using APES.Data;
using Discord;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using APES.Tournaments;

namespace APES
{
    public class SlashCommandsHandler : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("ape", "Show Help")]
        public async Task Help()
        {
            SocketGuild? guild = (Context.Channel as SocketGuildChannel)?.Guild;
            if (guild == null) return;

            Data.GuildSettings? guildSettings = DatabaseServices.TryGetCachedGuildSettings(guild);
            if (guildSettings == null) return;

            await RespondAsync(embed: EmbedFactory.BuildHelpEmbed(guildSettings, Program.Config.helpText), components: ButtonFactory.BuildHelpButtons(true), ephemeral: true);
        }

        [SlashCommand("use_reactions", "Should the APES respond to various trigger words like GG")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        public async Task SetUseReactions([Summary(description: "Activate or Deactivate reactions")] bool useReactions)
        {
            GuildSettings? settings = await DatabaseServices.GetGuildSettingsAsync(Context.Guild);
            GuildSettings? cachedSettings = DatabaseServices.TryGetCachedGuildSettings(Context.Guild);

            if (settings == null) return;

            settings.UseReactions = useReactions;

            if (cachedSettings != null)
                cachedSettings.UseReactions = useReactions;

            await DatabaseServices.SaveDB();

            await RespondAsync($"Reactions are {(useReactions ? "On" : "Off")}");
        }

        [SlashCommand("fight", "Start a defualt 2 teams match")]
        public async Task MatchTeamCount()
        {
            int[] config = { 2 };
            bool preTeams = false;
            bool limitMatch = false;

            var channel = (SocketTextChannel)Context.Channel;
            var owner = Context.User;

            await MatchServices.AddMatchFromSlash(channel, owner, config, limitMatch, preTeams, Program.matches);
            await RespondAsync($"4v4 Match started", ephemeral: true);
        }

        [SlashCommand("fight_team_count", "Start a match by specifying the number of teams")]
        public async Task MatchTeamCount(
        [Summary(description: "Number of teams")] int teams = 2,
        [Summary(description: "Use pre-made teams?")] bool preAssignedTeams = false)
        {
            int[] config = { teams };
            bool limitMatch = false;

            var channel = (SocketTextChannel)Context.Channel;
            var owner = Context.User;

            await MatchServices.AddMatchFromSlash(channel, owner, config, limitMatch, preAssignedTeams, Program.matches);
            await RespondAsync($"Match started with {teams}{(preAssignedTeams ? " premade" : "")} teams", ephemeral: true);
        }

        [SlashCommand("fight_team_size", "Start a match by specifying team size (3v3, 1v3 etc...)")]
        public async Task MatchTeamSize(
            [Summary(description: "Type team sizes seperated by v, ie; 2v2")] string format = "4v4",
            [Summary(description: "2 teams only?")] bool twoTeamsOnly = true)
        {
            var lowerCaseCommand = format.ToLower();
            var vsMatch = Regex.Match(lowerCaseCommand, @"(\d+)v(\d+)");
            if (vsMatch.Success)
            {
                int left = int.TryParse(vsMatch.Groups[1].Value, out var l) ? l : 0;
                int right = int.TryParse(vsMatch.Groups[2].Value, out var r) ? r : 0;
                int[] config = { left, right };
                bool preTeams = false;

                var channel = (SocketTextChannel)Context.Channel;
                var owner = Context.User;

                await MatchServices.AddMatchFromSlash(channel, owner, config, twoTeamsOnly, preTeams, Program.matches);
                await RespondAsync($"{left}v{right} Match started{(!twoTeamsOnly ? " with no team count limit" : "")}", ephemeral: true);
            }
            else
            {
                await RespondAsync($"Specify a valid format #v#, 3v3, 1v2 etc...", ephemeral: true);
                return;
            }

        }

        [SlashCommand("create_tournament", "Create a new tournament")]
        public async Task CreateTournament(string name, string description, TournamentType type)
        {
            await DeferAsync();
            var tournamentData = TournamentServices.CreateTournamentData(name, description, type, TournamentState.InSetup);
            tournamentData.GuildDataId = Program.DB.Guilds.First(g => g.GuildId == Context.Guild.Id).Id;

            Program.DB.TournamentDatas.Add(tournamentData);
            await Program.DB.SaveChangesAsync();

            await FollowupAsync($"Created Tournament {name}");
        }

        [SlashCommand("remove_tournament", "Remove a tournament")]
        public async Task RemoveTournament([Autocomplete] int tournament)
        {
            await DeferAsync();
            var touny = Program.DB.TournamentDatas.FirstOrDefault(t => t.Id == tournament);
            string name = "";
            if (touny != null)
            {
                name = touny.Name!;
                Program.DB.TournamentDatas.Remove(touny);
            }

            await Program.DB.SaveChangesAsync();

            await FollowupAsync($"Tournament {name} Deleted");
        }

        [SlashCommand("edit_tournament", "Edit a tournament's properties")]
        public async Task EditTournament(
                            [Autocomplete] int tournament,
                            [Summary(description: "Tournament name")] string? name = null,
                            [Summary(description: "Tournament description")] string? description = null,
                            [Summary(description: "Tournament type (e.g. SingleElimination, Season)")] TournamentType? type = null,
                            [Summary(description: "The State of the tournament")] TournamentState? state = null,
                            [Summary(description: "Number of players per team")] int teamSize = 0,
                            [Summary(description: "Does this tournament requires to register loadouts, 0 = no, >0 the amount of allowed loadouts")] int loadouts = 0,
                            [Summary(description: "UTC (game time) as yyyy-MM-dd HH:mm - e.g: 2025-08-26 17:36")] string? startDate = null,
                            [Summary(description: "UTC (game time) as yyyy-MM-dd HH:mm - e.g: 2025-08-26 17:36")] string? endDate = null,
                            [Summary(description: "UTC (game time) as yyyy-MM-dd HH:mm - e.g: 2025-08-26 17:36")] string? closeSignup = null,
                            [Summary(description: "Minimum matches per participant")] int minMatches = 0,
                            [Summary(description: "Maximum matches per participant")] int maxMatches = 0,
                            [Summary(description: "Allowed rank gap percentage between participants")] float maxRankGap = 0,
                            [Summary(description: "Number of matches before rank gap limit applies")] int rankGapMatchesThreshold = 0,
                            [Summary(description: "How many times a player can fight the same opponent")] int sameOpponentLimit = 0,
                            [Summary(description: "Percentage of opponents pool before reset of same-opponent limit")] float sameOpponenReset = 0,
                            [Summary(description: "Maximum matches allowed per timeframe per participant")] int maxMatchesPerTimeframe = 0,
                            [Summary(description: "Timeframe limit in days")] int timeFrameLimit = 0,
                            [Summary(description: "Max Elo points gained/lost per match")] int eloMaxPointsChange = 40,
                            [Summary(description: "Elo scaling factor (usually 400)")] int eloScale = 400,
                            [Summary(description: "Starting Elo rank for new participants")] int eloStartRank = 1500)
        {
            await DeferAsync();

            var db = Program.DB;
            var tournamentData = await db.TournamentDatas.FindAsync(tournament);

            if (tournamentData == null)
            {
                await FollowupAsync($"❌ Tournament with ID `{tournament}` not found.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(name)) tournamentData.Name = name;
            if (!string.IsNullOrWhiteSpace(description)) tournamentData.Description = description;
            if (type.HasValue) tournamentData.Type = (int)type.Value;
            if (teamSize > 0) tournamentData.TeamSize = teamSize;
            if (loadouts > 0) tournamentData.RequiredLoadouts = loadouts;

            if (state.HasValue) tournamentData.State = (int)state.Value;

            if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var sdt))
                tournamentData.Start = sdt;
            if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var edt))
                tournamentData.End = edt;
            if (!string.IsNullOrWhiteSpace(closeSignup) && DateTime.TryParse(closeSignup, out var csdt))
                tournamentData.CloseSignup = csdt;

            if (minMatches > 0) tournamentData.MinMatches = minMatches;
            if (maxMatches > 0) tournamentData.MaxMatches = maxMatches;
            if (maxRankGap > 0) tournamentData.MaxRankGap = maxRankGap;
            if (rankGapMatchesThreshold > 0) tournamentData.RankGapMatchesThreshold = rankGapMatchesThreshold;
            if (sameOpponentLimit > 0) tournamentData.SameOpponentLimit = sameOpponentLimit;
            if (sameOpponenReset > 0) tournamentData.SameOpponenReset = sameOpponenReset;
            if (maxMatchesPerTimeframe > 0) tournamentData.MaxMatchesPerTimeframe = maxMatchesPerTimeframe;
            if (timeFrameLimit > 0) tournamentData.TimeFrameLimit = timeFrameLimit;

            if (eloMaxPointsChange != 40) tournamentData.EloMaxPointsChange = eloMaxPointsChange;
            if (eloScale != 400) tournamentData.EloScale = eloScale;
            if (eloStartRank != 1500) tournamentData.EloStartRank = eloStartRank;

            await db.SaveChangesAsync();

            await FollowupAsync($"✅ Tournament **{tournamentData.Name ?? tournamentData.Id.ToString()}** was updated successfully.", embed: TournamentEmbedFactory.BuildTournamentSetupEmbed(tournamentData));
        }

        [SlashCommand("register_team", "Register a team for a tournament")]
        public async Task RegisterTeam(
                            [Autocomplete] int tournament,
                            [Summary(description: "Team name")] string teamName,
                            [Summary(description: "Team Captain")] IUser captain,
                            [Summary(description: "Team member 2 (optional)")] IUser? member2 = null,
                            [Summary(description: "Team member 3 (optional)")] IUser? member3 = null,
                            [Summary(description: "Team member 4 (optional)")] IUser? member4 = null)
        {
            await DeferAsync();

            var db = Program.DB;
            var tournamentData = await db.TournamentDatas!
                .Include(t => t.Teams!)
                    .ThenInclude(tm => tm.Members)
                .Include(t => t.Participants)
                .FirstOrDefaultAsync(t => t.Id == tournament);

            if (tournamentData == null)
            {
                await FollowupAsync($"❌ Tournament with ID `{tournament}` not found.");
                return;
            }

            // Collect members
            var users = new List<IUser> { captain };
            if (member2 != null) users.Add(member2);
            if (member3 != null) users.Add(member3);
            if (member4 != null) users.Add(member4);

            if (users.Count > tournamentData.TeamSize)
            {
                await FollowupAsync($"❌ Registration Failed.\nMaximum {tournamentData.TeamSize} members per team, please try again");
                return;
            }

            // Check if any user is already registered
            var registeredUserIds = tournamentData.Participants?.Select(p => p.DiscordUserDataId).ToHashSet() ?? new HashSet<int>();
            var alreadyRegistered = users.Where(u => registeredUserIds.Contains((int)u.Id)).ToList();

            if (alreadyRegistered.Any())
            {
                var names = string.Join(", ", alreadyRegistered.Select(u => u.Username));
                await FollowupAsync($"❌ These users are already registered in a team: {names}");
                return;
            }

            int startingRank = tournamentData.EloStartRank;
            if (tournamentData.Start.HasValue && tournamentData.Start.Value <= DateTime.UtcNow)
                startingRank = tournamentData.Teams?.Min(p => p.Rank) ?? tournamentData.EloStartRank;

            var team = new TeamData
            {
                Name = teamName,
                TournamentDataId = tournamentData.Id,
                TournamentData = tournamentData,
                Rank = startingRank,
                Members = new List<Participant>()
            };

            foreach (var user in users)
            {
                var participant = new Participant
                {
                    DiscordUserDataId = (int)user.Id,
                    TournamentDataId = tournamentData.Id,
                    TournamentData = tournamentData,
                    Rank = startingRank,
                    MatchesWon = 0,
                    MatchesLost = 0
                };
                team.Members.Add(participant);

                // Add to global tournament participant list
                tournamentData.Participants ??= new List<Participant>();
                tournamentData.Participants.Add(participant);
            }

            // Add team to tournament
            tournamentData.Teams ??= new List<TeamData>();
            tournamentData.Teams.Add(team);

            await db.SaveChangesAsync();

            var mentions = string.Join(", ", users.Select(u => $"<@{u.Id}>"));
            await FollowupAsync($"✅ Team **{teamName}** successfully registered with {users.Count} member(s): {mentions}");
        }
    }
}
/*
            int teamSize,
            [Summary(description: "UTC(game time) as yyyy-MM-dd HH:mm - e.g: 2025-08-26 17:36")]
            string? startDate = null,
            [Summary(description: "UTC(game time) as yyyy-MM-dd HH:mm - e.g: 2025-08-26 17:36")]
            string? endDate = null,
            [Summary(description: "UTC(game time) as yyyy-MM-dd HH:mm - e.g: 2025-08-26 17:36")]
            string? closeSignup = null,
            string description = "",
            int minMatches = 0,
            int maxMatches = 999,
            float maxRankGap = 0,
            int rankGapMatchesThreshold = 0,
            int sameOpponentLimit = 0,
            float sameOpponenReset = 0

            DateTime? start = null;
            if (startDate != null) 
                start = DateTime.ParseExact(startDate, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

            DateTime? end = null;
            if (startDate != null)
                end = DateTime.ParseExact(endDate, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

            DateTime? signupClose = null;
            if (startDate != null)
                signupClose = DateTime.ParseExact(closeSignup, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

*/
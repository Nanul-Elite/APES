// APES is free and open-source software licensed under AGPL-3.0. See LICENSE file for details.
using Discord.Interactions;
using Discord.WebSocket;
using System.Text.RegularExpressions;
using APES.Data;
using Discord;
using System.Globalization;

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

            if(cachedSettings != null)
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
        public async Task RemoveTournament([Autocomplete]int tournament)
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
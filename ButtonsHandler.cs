// APES is free and open-source software licensed under AGPL-3.0. See LICENSE file for details.
using APES.Data;
using Discord;
using Discord.WebSocket;
using System.Collections.Concurrent;
using static APES.Program;

namespace APES
{
	public class ButtonsHandler
	{
		public const string joinId = "join_match";
		public const string leaveId = "leave_match";
		public const string rollId = "roll_teams";
		public const string startMatchId = "start_match";
		public const string swapPlayersId = "swap_players";
        public const string endMatchId = "end_match";
        public const string removeMatchId = "remove_match";
        public const string backId = "back";
        public const string closeHelpId = "close_help";
		public const string team1Id = "team1_win";
        public const string team2Id = "team2_win";
        public const string splitId = "split";
        public const string typeHelpId = "typeHelp";
        public const string swapHelpId = "swapHelp";
        public const string leadersHelpId = "leadersHelp";
        public const string splitHelpId = "splitHelp";
        public const string dataHelpId = "dataHelp";
        public const string hideDataId = "hideData";
        public const string optOutDataId = "optOutData";
        public const string optInDataId = "optInData";
        public const string removeDataId = "removeData";
        public const string dataOptionsId = "dataOptions";

        private ConcurrentDictionary<ulong, MatchInstance> _matches;

        public ButtonsHandler(ConcurrentDictionary<ulong, MatchInstance> matches)
        {
            _matches = matches;
        }

        public async Task OnButtonExecuted(SocketMessageComponent component)
        {

            SocketGuild? guild = (component.Channel as SocketGuildChannel)?.Guild;
            if (guild == null) return;

            if (await HandleHelpButtons(component, guild))
                return;

            if (await HandleDataHelpButtons(component)) 
                return;

            await HandleMatchButtons(component, guild);
        }

        private async Task HandleMatchButtons(SocketMessageComponent component, SocketGuild guild)
        {
            SocketGuildUser guildUser = guild.GetUser(component.User.Id);

            if (!_matches.TryGetValue(component.Message.Id, out var match))
            {
                await component.RespondAsync("Brawl not found or expired.");
                return;
            }

            if (component.Data.CustomId == joinId)
            {
                await OnJoinPressed(match, component);
            }
            else if (component.Data.CustomId == leaveId)
            {
                await OnLeavePressed(match, component);
            }
            else if (guildUser != null && MatchServices.VarifyMatchPerm(match, guildUser))
            {
                if (component.Data.CustomId == rollId)
                {
                    await OnRollPressed(match, component);
                }
                else if (component.Data.CustomId == removeMatchId)
                {
                    await OnRemovePressed(component);
                }
                else if (component.Data.CustomId == startMatchId)
                {
                    await OnStartPressed(match, component);
                }
                else if (component.Data.CustomId == endMatchId && guildUser != null)
                {
                    await OnEndPressed(match, component);
                }
                else if (component.Data.CustomId == swapPlayersId && guildUser != null)
                {
                    await OnSwapPressed(match, component);
                }
                else if ((component.Data.CustomId == team1Id || component.Data.CustomId == team2Id) && guildUser != null)
                {
                    int winningTeam = component.Data.CustomId == team1Id ? 0 : 1;
                    await OnWinnerSelected(winningTeam, match, component, guildUser);
                }
                else if (component.Data.CustomId == backId)
                {
                    await OnBackPressed(match, component);
                }
                else if (component.Data.CustomId == splitId)
                {
                    await OnSplitPressed(match, component);
                }
            }
        }

        private static async Task<bool> HandleDataHelpButtons(SocketMessageComponent component)
        {
            if (component.Data.CustomId == dataOptionsId)
            {
                await component.RespondAsync("Choose your preference", ephemeral: true, components: ButtonFactory.BuildDataCollectionButtons());
                return true;
            }
            else if (component.Data.CustomId == hideDataId)
            {
                await DatabaseServices.HidePlayerScore(component.User);
                await component.RespondAsync("Your rank and score are now hidden", ephemeral: true);
                return true;
            }
            else if (component.Data.CustomId == optOutDataId)
            {
                bool succeeded = await DatabaseServices.RemoveAllPlayerData(component.User, true);
                if (succeeded)
                    await component.RespondAsync("Your rank, match history, and username have been deleted.\nYour opt-out preference has been saved", ephemeral: true);
                else
                    await component.RespondAsync("The user does not exist in the database", ephemeral: true);

                return true;
            }
            else if (component.Data.CustomId == removeDataId)
            {
                bool succeeded = await DatabaseServices.RemoveAllPlayerData(component.User, false);
                if (succeeded)
                    await component.RespondAsync("Your data has been deleted", ephemeral: true);
                else
                    await component.RespondAsync("The user does not exist in the database", ephemeral: true);

                return true;
            }
            else if (component.Data.CustomId == optInDataId)
            {
                await DatabaseServices.OptInData(component.User);
                await component.RespondAsync("Your data will now be saved.\nYour score and rank will be visible in leaderboards and match summaries from now on.", ephemeral: true);
                return true;
            }

            return false;
        }

        private static async Task<bool> HandleHelpButtons(SocketMessageComponent component, SocketGuild? guild)
        {
            GuildSettings? guildSettings = DatabaseServices.TryGetCachedGuildSettings(guild);

            if (component.Data.CustomId == closeHelpId)
            {
                await component.Message.DeleteAsync();
                return true;
            }
            else if (component.Data.CustomId == typeHelpId)
            {
                await component.RespondAsync(embed: EmbedFactory.BuildHelpEmbed(guildSettings, Config.typeText), ephemeral: true);
                return true;
            }
            else if (component.Data.CustomId == swapHelpId)
            {
                await component.RespondAsync(embed: EmbedFactory.BuildHelpEmbed(guildSettings, Config.swapText), ephemeral: true);
                return true;
            }
            else if (component.Data.CustomId == splitHelpId)
            {
                await component.RespondAsync(embed: EmbedFactory.BuildHelpEmbed(guildSettings, Config.splitText), ephemeral: true);
                return true;
            }
            else if (component.Data.CustomId == leadersHelpId)
            {
                await component.RespondAsync(embed: EmbedFactory.BuildHelpEmbed(guildSettings, Config.leaderText), ephemeral: true);
                return true;
            }
            else if (component.Data.CustomId == dataHelpId)
            {
                await component.RespondAsync(embed: EmbedFactory.BuildHelpEmbed(guildSettings, Config.dataCollectionText), components: ButtonFactory.BuildDataHelpButtons(), ephemeral: true);
                return true;
            }

            return false;
        }

        private async Task OnJoinPressed(MatchInstance match, SocketMessageComponent component)
        {
            if (!MatchServices.CheckVacancy(match)) return; // if max player has been reached

            bool isNewJoiner = !match.PlayerList.Contains(component.User.Id);
            ulong newJoinerId = component.User.Id;
            if (isNewJoiner)
            {
                match.PlayerList.Add(component.User.Id);
            }
            else if(Config.debug)
            {
                await MatchServices.AddFakeUserForDebug(match, component);
            }

            var timeoutMinutes = 15;
            match.TimeoutAt = DateTime.UtcNow.AddMinutes(timeoutMinutes);

            await component.DeferAsync();
            await match.Message.ModifyAsync(async m => m.Embed = await EmbedFactory.BuildMatchEmbed(match));
        }

        private async Task OnLeavePressed(MatchInstance match, SocketMessageComponent component)
		{
            if (match.PlayerList.Contains(component.User.Id))
                match.PlayerList.Remove(component.User.Id);

            for (int i = 0; i < match.Teams.Count; i++)
            {
                if (match.Teams[i].Contains(component.User.Id))
                    match.Teams[i].Remove(component.User.Id);
            }

            if (match.PlayerList.Count < 2)
            {
                match.state = MatchState.open;
            }

            await component.DeferAsync();
            await match.Message.ModifyAsync(async m => { m.Embed = await EmbedFactory.BuildMatchEmbed(match); m.Components = ButtonFactory.BuildMatchButtons(match); });
        }

        private async Task OnRollPressed(MatchInstance match, SocketMessageComponent component)
        {
            if (match.PlayerList.Count < 2)
            {
                return;
            }
            var teams = MatchServices.DividePlayersIntoTeams(match);

            match.Teams = teams;

            var timeoutMinutes = 60;
            match.TimeoutAt = DateTime.UtcNow.AddMinutes(timeoutMinutes);

            await component.DeferAsync();
            await match.Message.ModifyAsync(async m => { m.Embed = await EmbedFactory.BuildMatchEmbed(match); m.Components = ButtonFactory.BuildMatchButtons(match); });
        }

        private async Task OnStartPressed(MatchInstance match, SocketMessageComponent component)
        {
            if (match.Teams.Count < 2)
            {
                return;
            }

            var timeoutMinutes = 60;
            match.TimeoutAt = DateTime.UtcNow.AddMinutes(timeoutMinutes);

            match.state = MatchState.locked;

            await component.DeferAsync();
            await match.Message.ModifyAsync(async m => { m.Embed = await EmbedFactory.BuildMatchEmbed(match); m.Components = ButtonFactory.BuildMatchButtons(match); });
        }

        private async Task OnEndPressed(MatchInstance match, SocketMessageComponent component)
        {
            var timeoutMinutes = 60;
            match.TimeoutAt = DateTime.UtcNow.AddMinutes(timeoutMinutes);

            if (match.Teams.Count == 2)
                match.state = MatchState.waitingForResults;
            else
                match.state = MatchState.rolled;


            await component.DeferAsync();
            await match.Message.ModifyAsync(async m => { m.Embed = await EmbedFactory.BuildMatchEmbed(match); m.Components = ButtonFactory.BuildMatchButtons(match); });
        }

        private async Task OnSwapPressed(MatchInstance match, SocketMessageComponent component)
        {
            var modal = new ModalBuilder()
                    .WithTitle("Swap Players")
                    .WithCustomId($"{match.matchId}")
                    .AddTextInput("Player's Team Numbers", "names_input", placeholder: "3:1 (the number next to the players)");

            await component.RespondWithModalAsync(modal.Build());
        }

        private async Task OnRemovePressed(SocketMessageComponent component)
		{
            await MatchServices.RemoveMatchAsync(component.Message, _matches);
        }

        private async Task OnBackPressed(MatchInstance match, SocketMessageComponent component)
        {
            var timeoutMinutes = 60;
            match.TimeoutAt = DateTime.UtcNow.AddMinutes(timeoutMinutes);

            match.state = MatchState.rolled;

            await component.DeferAsync();
            await match.Message.ModifyAsync(async m => { m.Embed =  await EmbedFactory.BuildMatchEmbed(match); m.Components = ButtonFactory.BuildMatchButtons(match); });
        }

        private async Task OnSplitPressed(MatchInstance match, SocketMessageComponent component)
        {
            await MatchServices.SplitMatch(match, component.Message, _matches);

            await component.DeferAsync();
            await match.Message.ModifyAsync(async m => { m.Embed = await EmbedFactory.BuildMatchEmbed(match); m.Components = ButtonFactory.BuildMatchButtons(match); });
        }

        private async Task OnWinnerSelected(int winningTeam, MatchInstance match, SocketMessageComponent component, SocketGuildUser guildUser)
        {
            int losingTeam = Math.Abs(winningTeam - 1);
            bool acceptVote = guildUser.GuildPermissions.Administrator || guildUser.GuildPermissions.ModerateMembers || match.Teams[losingTeam].Contains(guildUser.Id);
            if (acceptVote)
            {
                SocketGuild guild = guildUser.Guild;
                ulong guildId = guildUser.Guild.Id;
                GuildData? guildData = await DatabaseServices.GetGuildData(guildId);
                if (guildData == null) return;

                List<List<Participant>> teamsParticipantData = new();
                TournamentData? tournamentData = await DatabaseServices.GetFullTournamentDataAsync(guildId);
                if (tournamentData == null) return;

                List<Participant>? allTournamentParticipantsData = tournamentData.Participants;
                if (allTournamentParticipantsData == null) return;

                foreach (var team in match.Teams)
                {
                    var teamData = new List<Participant>();

                    foreach (var userId in team)
                    {
                        DiscordUserData? userData = await DatabaseServices.GetOrCreateDiscordUserData(guild, userId);
                        if (userData == null) continue;

                        Participant? participant = allTournamentParticipantsData.FirstOrDefault(p => p.DiscordUserDataId == userData.Id);

                        if (participant == null)
                        {
                            participant = new Participant
                            {
                                DiscordUserDataId = userData.Id,
                                TournamentDataId = guildData.Id,
                                Rank = 1500,
                                MatchesWon = 0,
                                MatchesLost = 0
                            };

                            await DatabaseServices.AddParticipantToTournamentData(tournamentData, participant);
                            allTournamentParticipantsData.Add(participant);
                        }

                        teamData.Add(participant);
                    }

                    teamsParticipantData.Add(teamData);
                }

                (int score, float expected) = RankServices.ApplyMatchResults(teamsParticipantData[winningTeam], teamsParticipantData[losingTeam]);
                await DatabaseServices.SaveDB();

                match.state = MatchState.done;

                await component.DeferAsync();
                await match.Message.ModifyAsync(async m => { m.Embed = await EmbedFactory.BuildMatchEndEmbed(teamsParticipantData[winningTeam], teamsParticipantData[losingTeam], score, expected, guildId); m.Components = new ComponentBuilder().Build(); });
                await MatchServices.RemoveMatchAsync(match.Message, _matches);
            }
            else
            {
                await component.RespondAsync("Only the losing team, mods or admins can set the result");
            }
        }
    }
}
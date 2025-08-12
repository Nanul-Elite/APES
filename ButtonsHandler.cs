// APES is free and open-source software licensed under AGPL-3.0. See LICENSE file for details.
using APES.Data;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text.RegularExpressions;
using static APES.Program;

namespace APES
{
	public class ButtonsHandler
	{
        public delegate Task ButtonHandler(SocketMessageComponent component);
        private Dictionary<(string category, string action), ButtonHandler> _handlers = new();

        private ConcurrentDictionary<ulong, MatchInstance> _matches;

        public ButtonsHandler(ConcurrentDictionary<ulong, MatchInstance> matches)
        {
            _matches = matches;

            _handlers = new Dictionary<(string category, string action), ButtonHandler>()
            {
                {(ButtonCategories.Match, MatchActions.Join), OnJoinPressed },
                {(ButtonCategories.Match, MatchActions.Leave), OnLeavePressed },
                {(ButtonCategories.Match, MatchActions.Roll), OnRollPressed },
                {(ButtonCategories.Match, MatchActions.Remove), OnRemovePressed },
                {(ButtonCategories.Match, MatchActions.Start), OnStartPressed },
                {(ButtonCategories.Match, MatchActions.End), OnEndPressed },
                {(ButtonCategories.Match, MatchActions.Swap), OnSwapPressed },
                {(ButtonCategories.Match, MatchActions.Split), OnSplitPressed },
                {(ButtonCategories.Match, MatchActions.Back), OnBackPressed },
                {(ButtonCategories.Match, MatchActions.Team1), OnWinnerSelected },
                {(ButtonCategories.Match, MatchActions.Team2), OnWinnerSelected },

                {(ButtonCategories.Help, HelpActions.Type), OnTypeHelpPressed },
                {(ButtonCategories.Help, HelpActions.Swap), OnSwapHelpPressed },
                {(ButtonCategories.Help, HelpActions.Split), OnSplitHelpPressed },
                {(ButtonCategories.Help, HelpActions.Leaders), OnLeadersHelpPressed },
                {(ButtonCategories.Help, HelpActions.Data), OnDataHelpPressed },

                {(ButtonCategories.Data, DataActions.Options), OnDataOptionsPressed },
                {(ButtonCategories.Data, DataActions.OptIn), OnOptInDataPressed },
                {(ButtonCategories.Data, DataActions.OptOut), OnOptOutDataPressed },
                {(ButtonCategories.Data, DataActions.Hide), OnHideDataPressed },
                {(ButtonCategories.Data, DataActions.Delete), OnDeleteDataPressed },

                {(ButtonCategories.Common, CommonActions.Close), OnClosePressed },
            };
        }

        public async Task HandleButtonsAsync(SocketMessageComponent component)
        {
            var parts = component.Data.CustomId.Split(':');
            if (parts.Length < 2) return;

            var category = parts[0];
            var action = parts[1];

            if (_handlers.TryGetValue((category, action), out var handler))
            {
                await handler(component);
            }
        }

        private async Task<MatchInstance?> GetMatch(SocketMessageComponent component)
        {
            if (!_matches.TryGetValue(component.Message.Id, out var match))
            {
                await component.RespondAsync("Brawl not found or expired.");
                return null;
            }
            else
            {
                return match;
            }
        }

        private (SocketGuild?, SocketGuildUser?) GetGuildAndGuildUser(SocketMessageComponent component)
        {
            SocketGuild? guild = (component.Channel as SocketGuildChannel)?.Guild;
            if (guild == null) return (null, null);

            SocketGuildUser guildUser = guild.GetUser(component.User.Id);
            return (guild, guildUser);
        }

        // Common Buttons
        private async Task OnClosePressed(SocketMessageComponent component)
        {
            await component.Message.DeleteAsync();
        }

        // Help Buttons
        private async Task OnTypeHelpPressed(SocketMessageComponent component)
        {
            SocketGuild? guild = (component.Channel as SocketGuildChannel)?.Guild;
            if (guild == null) return;

            GuildSettings? guildSettings = DatabaseServices.TryGetCachedGuildSettings(guild);
            if (guildSettings == null) return;

            await component.RespondAsync(embed: EmbedFactory.BuildHelpEmbed(guildSettings, Config.typeText), ephemeral: true);
        }

        private async Task OnSplitHelpPressed(SocketMessageComponent component)
        {
            SocketGuild? guild = (component.Channel as SocketGuildChannel)?.Guild;
            if (guild == null) return;

            GuildSettings? guildSettings = DatabaseServices.TryGetCachedGuildSettings(guild);
            if (guildSettings == null) return;

            await component.RespondAsync(embed: EmbedFactory.BuildHelpEmbed(guildSettings, Config.splitText), ephemeral: true);
        }

        private async Task OnSwapHelpPressed(SocketMessageComponent component)
        {
            SocketGuild? guild = (component.Channel as SocketGuildChannel)?.Guild;
            if (guild == null) return;

            GuildSettings? guildSettings = DatabaseServices.TryGetCachedGuildSettings(guild);
            if (guildSettings == null) return;

            await component.RespondAsync(embed: EmbedFactory.BuildHelpEmbed(guildSettings, Config.swapText), ephemeral: true);
        }

        private async Task OnLeadersHelpPressed(SocketMessageComponent component)
        {
            SocketGuild? guild = (component.Channel as SocketGuildChannel)?.Guild;
            if (guild == null) return;

            GuildSettings? guildSettings = DatabaseServices.TryGetCachedGuildSettings(guild);
            if (guildSettings == null) return;

            await component.RespondAsync(embed: EmbedFactory.BuildHelpEmbed(guildSettings, Config.leaderText), ephemeral: true);
        }

        // Data Buttons
        private async Task OnDataHelpPressed(SocketMessageComponent component)
        {
            SocketGuild? guild = (component.Channel as SocketGuildChannel)?.Guild;
            if (guild == null) return;

            GuildSettings? guildSettings = DatabaseServices.TryGetCachedGuildSettings(guild);
            if (guildSettings == null) return;

            await component.RespondAsync(embed: EmbedFactory.BuildHelpEmbed(guildSettings, Config.dataCollectionText), components: ButtonFactory.BuildDataHelpButtons(), ephemeral: true);
        }

        private async Task OnDataOptionsPressed(SocketMessageComponent component)
        {
            await component.RespondAsync("Choose your preference", ephemeral: true, components: ButtonFactory.BuildDataCollectionButtons());
        }

        private async Task OnHideDataPressed(SocketMessageComponent component)
        {
            await DatabaseServices.HidePlayerScore(component.User);
            await component.RespondAsync("Your rank and score are now hidden", ephemeral: true);
        }

        private async Task OnOptOutDataPressed(SocketMessageComponent component)
        {
            bool succeeded = await DatabaseServices.RemoveAllPlayerData(component.User, true);
            if (succeeded)
                await component.RespondAsync("Your rank, match history, and username have been deleted.\nYour opt-out preference has been saved", ephemeral: true);
            else
                await component.RespondAsync("The user does not exist in the database", ephemeral: true);
        }

        private async Task OnDeleteDataPressed(SocketMessageComponent component)
        {
            bool succeeded = await DatabaseServices.RemoveAllPlayerData(component.User, false);
            if (succeeded)
                await component.RespondAsync("Your data has been deleted", ephemeral: true);
            else
                await component.RespondAsync("The user does not exist in the database", ephemeral: true);
        }

        private async Task OnOptInDataPressed(SocketMessageComponent component)
        {
            await DatabaseServices.OptInData(component.User);
            await component.RespondAsync("Your data will now be saved.\nYour score and rank will be visible in leaderboards and match summaries from now on.", ephemeral: true);
        }

        // Match Buttons
        private async Task OnJoinPressed(SocketMessageComponent component)
        {
            MatchInstance? match = await GetMatch(component);
            if (match == null)
                return;

            if (!MatchServices.CheckVacancy(match))
                return; // if max player has been reached

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

        private async Task OnLeavePressed(SocketMessageComponent component)
		{
            MatchInstance? match = await GetMatch(component);
            if (match == null)
                return;

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

        private async Task OnRollPressed(SocketMessageComponent component)
        {
            MatchInstance? match = await GetMatch(component);
            if (match == null) return;

            (SocketGuild? guild, SocketGuildUser? guildUser) = GetGuildAndGuildUser(component);
            if (guild == null || guildUser == null) return;

            if (!MatchServices.VarifyMatchPerm(match, guildUser)) return;

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

        private async Task OnStartPressed(SocketMessageComponent component)
        {
            MatchInstance? match = await GetMatch(component);
            if (match == null) return;

            (SocketGuild? guild, SocketGuildUser? guildUser) = GetGuildAndGuildUser(component);
            if (guild == null || guildUser == null) return;

            if (!MatchServices.VarifyMatchPerm(match, guildUser)) return;

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

        private async Task OnEndPressed(SocketMessageComponent component)
        {
            MatchInstance? match = await GetMatch(component);
            if (match == null) return;

            (SocketGuild? guild, SocketGuildUser? guildUser) = GetGuildAndGuildUser(component);
            if (guild == null || guildUser == null) return;

            if (!MatchServices.VarifyMatchPerm(match, guildUser)) return;

            var timeoutMinutes = 60;
            match.TimeoutAt = DateTime.UtcNow.AddMinutes(timeoutMinutes);

            if (match.Teams.Count == 2)
                match.state = MatchState.waitingForResults;
            else
                match.state = MatchState.rolled;


            await component.DeferAsync();
            await match.Message.ModifyAsync(async m => { m.Embed = await EmbedFactory.BuildMatchEmbed(match); m.Components = ButtonFactory.BuildMatchButtons(match); });
        }

        private async Task OnSwapPressed(SocketMessageComponent component)
        {
            MatchInstance? match = await GetMatch(component);
            if (match == null) return;
            
            (SocketGuild? guild, SocketGuildUser? guildUser) = GetGuildAndGuildUser(component);
            if (guild == null || guildUser == null) return;

            if (!MatchServices.VarifyMatchPerm(match, guildUser)) return;

            var modal = new ModalBuilder()
                    .WithTitle("Swap Players")
                    .WithCustomId($"{match.matchId}")
                    .AddTextInput("Player's Team Numbers", "names_input", placeholder: "3:1 (the number next to the players)");

            await component.RespondWithModalAsync(modal.Build());
        }

        private async Task OnRemovePressed(SocketMessageComponent component)
		{
            MatchInstance? match = await GetMatch(component);
            if (match == null) return;

            (SocketGuild? guild, SocketGuildUser? guildUser) = GetGuildAndGuildUser(component);
            if (guild == null || guildUser == null) return;

            if (!MatchServices.VarifyMatchPerm(match, guildUser)) return;

            await MatchServices.RemoveMatchAsync(component.Message, _matches);
        }

        private async Task OnBackPressed(SocketMessageComponent component)
        {
            MatchInstance? match = await GetMatch(component);
            if (match == null) return;

            (SocketGuild? guild, SocketGuildUser? guildUser) = GetGuildAndGuildUser(component);
            if (guild == null || guildUser == null) return;

            if (!MatchServices.VarifyMatchPerm(match, guildUser)) return;

            var timeoutMinutes = 60;
            match.TimeoutAt = DateTime.UtcNow.AddMinutes(timeoutMinutes);

            match.state = MatchState.rolled;

            await component.DeferAsync();
            await match.Message.ModifyAsync(async m => { m.Embed =  await EmbedFactory.BuildMatchEmbed(match); m.Components = ButtonFactory.BuildMatchButtons(match); });
        }

        private async Task OnSplitPressed(SocketMessageComponent component)
        {
            MatchInstance? match = await GetMatch(component);
            if (match == null) return;

            (SocketGuild? guild, SocketGuildUser? guildUser) = GetGuildAndGuildUser(component);
            if (guild == null || guildUser == null) return;

            if (!MatchServices.VarifyMatchPerm(match, guildUser)) return;

            await MatchServices.SplitMatch(match, component.Message, _matches);

            await component.DeferAsync();
            await match.Message.ModifyAsync(async m => { m.Embed = await EmbedFactory.BuildMatchEmbed(match); m.Components = ButtonFactory.BuildMatchButtons(match); });
        }

        private async Task OnWinnerSelected(SocketMessageComponent component)
        {
            MatchInstance? match = await GetMatch(component);
            if (match == null) return;
            
            int winningTeam = component.Data.CustomId == $"{ButtonCategories.Match}:{MatchActions.Team1}" ? 0 : 1;

            SocketGuild? guild = (component.Channel as SocketGuildChannel)?.Guild;
            if (guild == null) return;

            SocketGuildUser guildUser = guild.GetUser(component.User.Id);

            int losingTeam = Math.Abs(winningTeam - 1);
            bool acceptVote = guildUser.GuildPermissions.Administrator || guildUser.GuildPermissions.ModerateMembers || match.Teams[losingTeam].Contains(guildUser.Id);
            if (acceptVote)
            {
                ulong guildId = guild.Id;
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
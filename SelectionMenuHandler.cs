// APES is free and open-source software licensed under AGPL-3.0. See LICENSE file for details.
using Discord.WebSocket;
using System.Collections.Concurrent;

namespace APES
{
    public class SelectionMenuHandler
    {
        private ConcurrentDictionary<ulong, MatchInstance> _matches;
        
        public const string teamSelectID = "team_select";

        public SelectionMenuHandler(ConcurrentDictionary<ulong, MatchInstance> matches)
        {
            _matches = matches;
        }

        public async Task HandleDropdownAsync(SocketMessageComponent component)
        {
            if (!_matches.TryGetValue(component.Message.Id, out var match))
            {
                await component.RespondAsync("Brawl not found or expired.");
                return;
            }

            if (component.Data.CustomId == teamSelectID)
            {
                await OnTeamSelected(match, component);
            }

            await component.DeferAsync();
            await match.Message.ModifyAsync(async m => { m.Embed = await EmbedFactory.BuildMatchEmbed(match); m.Components = ButtonFactory.BuildMatchButtons(match); });
        }

        private async Task OnTeamSelected(MatchInstance match, SocketMessageComponent component)
        {
            int selectedTeam = int.Parse(component.Data.Values.First());
            SocketUser user = component.User;
            ulong userId = user.Id;

            bool isNewJoiner = !match.PlayerList.Contains(userId);
            if (isNewJoiner)
            {
                match.PlayerList.Add(userId);
            }
            else if (Program.Config.debug)
            {
                userId = await MatchServices.AddFakeUserForDebug(match, component);
            }
            else
            {
                return;
            }

            if(match.Teams.ElementAtOrDefault(selectedTeam) == null)
                match.Teams.Add(new List<ulong>());

            match.Teams[selectedTeam].Add(userId);
        }
    }
}

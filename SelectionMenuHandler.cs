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
            if (component.Data.CustomId == teamSelectID)
            {
                if(_matches.TryGetValue(component.Message.Id, out var match))
                {
                    await OnTeamSelected(match, component);
                    await match.Message.ModifyAsync(async m => { m.Embed = await EmbedFactory.BuildMatchEmbed(match); m.Components = ButtonFactory.BuildMatchButtons(match); });
                }
                else
                {
                    await component.RespondAsync("Brawl not found or expired.");
                }
            }

            if(component.Data.CustomId.StartsWith(ComponentCategories.SessionRequest))
            {
                var parts = component.Data.CustomId.Split(':');
                if (parts.Length < 3) return;

                var dropdown = parts[1];
                var guid = parts[2];

                if(Program.requestsInSetup.TryGetValue(guid, out var request))
                {
                    //if(dropdown == SessionRequestActions.Anonymous)
                    //{
                    //    request.Anonymous = bool.Parse(component.Data.Values.First());
                    //}
                    if (dropdown == SessionRequestActions.TimeZone)
                    {
                        request.TimeZone = component.Data.Values.First();
                    }
                    //else if (dropdown == SessionRequestActions.Date)
                    //{
                    //    request.Date = component.Data.Values.First();
                    //}
                    //else if(dropdown == SessionRequestActions.Duration)
                    //{
                    //    request.Duration = component.Data.Values.First();
                    //}
                    else if(dropdown == SessionRequestActions.Type)
                    {
                        request.SessionType = component.Data.Values.First();
                    }
                    else if(dropdown == SessionRequestActions.Level)
                    {
                        request.ExperienceLevel = int.Parse(component.Data.Values.First());
                    }

                    await component.UpdateAsync(m => { m.Embed = EmbedFactory.BuildSessionRequestEmbed(request); m.Components = ButtonFactory.BuildSessionRequestButtons(request); });
                }
            }

            await component.DeferAsync();
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

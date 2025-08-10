// APES is free and open-source software licensed under AGPL-3.0. See LICENSE file for details.
using Discord;

namespace APES
{
    internal class ButtonFactory
    {
        public static MessageComponent BuildMatchButtons(MatchInstance match)
        {
            var componentBuilder = new ComponentBuilder();

            if(match.state == MatchState.open || match.state == MatchState.rolled)
            {
                if(match.preTeams == false)
                    componentBuilder.WithButton("Join Match", ButtonsHandler.joinId, ButtonStyle.Success, row: 0, emote: new Emoji("✅"));
                componentBuilder.WithButton("Leave Match", ButtonsHandler.leaveId, ButtonStyle.Danger, row: 0, emote: new Emoji("✖️"));

                componentBuilder.WithButton("Roll Teams", ButtonsHandler.rollId, ButtonStyle.Primary, row: 1, emote: new Emoji("🎲"));
                componentBuilder.WithButton("Close Match", ButtonsHandler.removeMatchId, ButtonStyle.Secondary, row: 1, emote: new Emoji("🚫"));

                if(match.state == MatchState.rolled && match.Teams.Count == 2)
                {
                    componentBuilder.WithButton("Swap Players", ButtonsHandler.swapPlayersId, ButtonStyle.Secondary, row: 2, emote: new Emoji("🔀"));
                    componentBuilder.WithButton("Start Match", ButtonsHandler.startMatchId, ButtonStyle.Primary, row: 2, emote: new Emoji("🚩"));
                }
                else if (match.Teams.Count > 2)
                {
                    componentBuilder.WithButton("Split", ButtonsHandler.splitId, ButtonStyle.Secondary, row: 2, emote: new Emoji("↔️"));
                }

                if(match.preTeams == true)
                {
                    var menu = new SelectMenuBuilder()
                                    .WithCustomId(SelectionMenuHandler.teamSelectID)
                                    .WithPlaceholder("Choose a team");

                    for (int i = 0; i < match.MatchType[0]; i++)
                        menu.AddOption($"Team {i + 1}", i.ToString("0"));

                    componentBuilder.WithSelectMenu(menu);
                }
            }
            else if(match.state == MatchState.locked)
            {
                componentBuilder.WithButton("Back", ButtonsHandler.backId, ButtonStyle.Secondary, row: 0, emote: new Emoji("⬅️"));
                componentBuilder.WithButton("Close Match", ButtonsHandler.removeMatchId, ButtonStyle.Secondary, row: 0, emote: new Emoji("🚫"));

                componentBuilder.WithButton("End Match", ButtonsHandler.endMatchId, ButtonStyle.Primary, row: 1, emote: new Emoji("🏁"));
            }
            else if (match.state == MatchState.waitingForResults)
            {
                componentBuilder.WithButton("Back", ButtonsHandler.backId, ButtonStyle.Secondary, row: 0, emote: new Emoji("⬅️"));

                componentBuilder.WithButton("Team 1", ButtonsHandler.team1Id, ButtonStyle.Secondary, row: 1, emote: new Emoji("1️⃣"));
                componentBuilder.WithButton("Team 2", ButtonsHandler.team2Id, ButtonStyle.Secondary, row: 1, emote: new Emoji("2️⃣"));
            }
            else if (match.state == MatchState.done)
            {
                componentBuilder.WithButton("Back", ButtonsHandler.backId, ButtonStyle.Secondary, row: 0, emote: new Emoji("⬅️"));
                componentBuilder.WithButton("Roll Teams", ButtonsHandler.rollId, ButtonStyle.Primary, row: 0, emote: new Emoji("🎲"));
                componentBuilder.WithButton("Close Match", ButtonsHandler.removeMatchId, ButtonStyle.Secondary, row: 0, emote: new Emoji("🚫"));
            }

            return componentBuilder.Build();
        }

        public static MessageComponent BuildCloseButton()
        {
            return new ComponentBuilder().WithButton("Close", ButtonsHandler.closeHelpId, ButtonStyle.Secondary).Build();
        }

        public static MessageComponent BuildHelpButtons(bool ephemeral = false)
        {
            var componentBuilder = new ComponentBuilder();
            componentBuilder.WithButton("Match Types", ButtonsHandler.typeHelpId, ButtonStyle.Secondary, row: 0).Build();
            componentBuilder.WithButton("Swap Players", ButtonsHandler.swapHelpId, ButtonStyle.Secondary, row: 0).Build();
            componentBuilder.WithButton("Split Large Matchs", ButtonsHandler.splitHelpId, ButtonStyle.Secondary, row: 0).Build();
            componentBuilder.WithButton("Leader Board", ButtonsHandler.leadersHelpId, ButtonStyle.Secondary, row: 0).Build();

            componentBuilder.WithButton("Data Collection", ButtonsHandler.dataHelpId, ButtonStyle.Secondary, row: 1).Build();

            if(!ephemeral)
                componentBuilder.WithButton("Close", ButtonsHandler.closeHelpId, ButtonStyle.Secondary, row: 2).Build();

            return componentBuilder.Build();
        }

        public static MessageComponent BuildDataCollectionButtons()
        {
            var componentBuilder = new ComponentBuilder();
            componentBuilder.WithButton("Hide Score", ButtonsHandler.hideDataId, ButtonStyle.Success, row: 0).Build();
            componentBuilder.WithButton("Opt Out", ButtonsHandler.optOutDataId, ButtonStyle.Secondary, row: 0).Build();
            componentBuilder.WithButton("Delete All Data", ButtonsHandler.removeDataId, ButtonStyle.Danger, row: 0).Build();
            componentBuilder.WithButton("Opt In", ButtonsHandler.optInDataId, ButtonStyle.Primary, row: 0).Build();

            return componentBuilder.Build();
        }

        public static MessageComponent BuildDataHelpButtons()
        {
            var componentBuilder = new ComponentBuilder();
            componentBuilder.WithButton("Data Collection Options", ButtonsHandler.dataOptionsId, ButtonStyle.Primary, row: 0).Build();
            return componentBuilder.Build();
        }
    }
}

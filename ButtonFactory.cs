// APES is free and open-source software licensed under AGPL-3.0. See LICENSE file for details.
using Discord;

namespace APES
{
    internal class ButtonFactory
    {
        public static MessageComponent BuildMatchButtons(MatchInstance match)
        {
            var componentBuilder = new ComponentBuilder();

            string prefix = ButtonCategories.Match;

            if(match.state == MatchState.open || match.state == MatchState.rolled)
            {
                if(match.preTeams == false)
                    componentBuilder.WithButton("Join Match", $"{prefix}:{MatchActions.Join}", ButtonStyle.Success, row: 0, emote: new Emoji("✅"));
                componentBuilder.WithButton("Leave Match", $"{prefix}:{MatchActions.Leave}", ButtonStyle.Danger, row: 0, emote: new Emoji("✖️"));

                componentBuilder.WithButton("Roll Teams", $"{prefix}:{MatchActions.Roll}", ButtonStyle.Primary, row: 1, emote: new Emoji("🎲"));
                componentBuilder.WithButton("Close Match", $"{prefix}:{MatchActions.Remove}", ButtonStyle.Secondary, row: 1, emote: new Emoji("🚫"));

                if(match.state == MatchState.rolled && match.Teams.Count == 2)
                {
                    componentBuilder.WithButton("Swap Players", $"{prefix}:{MatchActions.Swap}", ButtonStyle.Secondary, row: 2, emote: new Emoji("🔀"));
                    componentBuilder.WithButton("Start Match", $"{prefix}:{MatchActions.Start}", ButtonStyle.Primary, row: 2, emote: new Emoji("🚩"));
                }
                else if (match.Teams.Count > 2)
                {
                    componentBuilder.WithButton("Split", $"{prefix}:{MatchActions.Split}", ButtonStyle.Secondary, row: 2, emote: new Emoji("↔️"));
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
                componentBuilder.WithButton("Back", $"{prefix}:{MatchActions.Back}", ButtonStyle.Secondary, row: 0, emote: new Emoji("⬅️"));
                componentBuilder.WithButton("Close Match", $"{prefix}:{MatchActions.Remove}", ButtonStyle.Secondary, row: 0, emote: new Emoji("🚫"));

                componentBuilder.WithButton("End Match", $"{prefix}:{MatchActions.End}", ButtonStyle.Primary, row: 1, emote: new Emoji("🏁"));
            }
            else if (match.state == MatchState.waitingForResults)
            {
                componentBuilder.WithButton("Back", $"{prefix}:{MatchActions.Back}", ButtonStyle.Secondary, row: 0, emote: new Emoji("⬅️"));

                componentBuilder.WithButton("Team 1", $"{prefix}:{MatchActions.Team1}", ButtonStyle.Secondary, row: 1, emote: new Emoji("1️⃣"));
                componentBuilder.WithButton("Team 2", $"{prefix}:{MatchActions.Team2}", ButtonStyle.Secondary, row: 1, emote: new Emoji("2️⃣"));
            }
            else if (match.state == MatchState.done)
            {
                componentBuilder.WithButton("Back", $"{prefix}:{MatchActions.Back}", ButtonStyle.Secondary, row: 0, emote: new Emoji("⬅️"));
                componentBuilder.WithButton("Roll Teams", $"{prefix}:{MatchActions.Roll}", ButtonStyle.Primary, row: 0, emote: new Emoji("🎲"));
                componentBuilder.WithButton("Close Match", $"{prefix}:{MatchActions.Remove}", ButtonStyle.Secondary, row: 0, emote: new Emoji("🚫"));
            }

            return componentBuilder.Build();
        }

        public static MessageComponent BuildCloseButton()
        {
            return new ComponentBuilder().WithButton("Close", $"{ButtonCategories.Common}:{CommonActions.Close}", ButtonStyle.Secondary).Build();
        }

        public static MessageComponent BuildHelpButtons(bool ephemeral = false)
        {
            string prefix = ButtonCategories.Help;
            var componentBuilder = new ComponentBuilder();
            componentBuilder.WithButton("Match Types", $"{prefix}:{HelpActions.Type}", ButtonStyle.Secondary, row: 0).Build();
            componentBuilder.WithButton("Swap Players", $"{prefix}:{HelpActions.Swap}", ButtonStyle.Secondary, row: 0).Build();
            componentBuilder.WithButton("Split Large Matchs", $"{prefix}:{HelpActions.Split}", ButtonStyle.Secondary, row: 0).Build();
            componentBuilder.WithButton("Leader Board", $"{prefix}:{HelpActions.Leaders}", ButtonStyle.Secondary, row: 0).Build();

            componentBuilder.WithButton("Data Collection", $"{prefix}:{HelpActions.Data}", ButtonStyle.Secondary, row: 1).Build();

            if(!ephemeral)
                componentBuilder.WithButton("Close", $"{ButtonCategories.Common}:{CommonActions.Close}", ButtonStyle.Secondary, row: 2).Build();

            return componentBuilder.Build();
        }

        public static MessageComponent BuildDataCollectionButtons()
        {
            string prefix = ButtonCategories.Data;

            var componentBuilder = new ComponentBuilder();
            componentBuilder.WithButton("Hide Score", $"{prefix}:{DataActions.Hide}", ButtonStyle.Success, row: 0).Build();
            componentBuilder.WithButton("Opt Out", $"{prefix}:{DataActions.OptOut}", ButtonStyle.Secondary, row: 0).Build();
            componentBuilder.WithButton("Delete All Data", $"{prefix}:{DataActions.Delete}", ButtonStyle.Danger, row: 0).Build();
            componentBuilder.WithButton("Opt In", $"{prefix}:{DataActions.OptIn}", ButtonStyle.Primary, row: 0).Build();

            return componentBuilder.Build();
        }

        public static MessageComponent BuildDataHelpButtons()
        {
            var componentBuilder = new ComponentBuilder();
            componentBuilder.WithButton("Data Collection Options", $"{ButtonCategories.Data}:{DataActions.Options}", ButtonStyle.Primary, row: 0).Build();
            return componentBuilder.Build();
        }
    }
}

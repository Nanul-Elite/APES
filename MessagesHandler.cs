// APES is free and open-source software licensed under AGPL-3.0. See LICENSE file for details.
using Discord.WebSocket;
using System.Collections.Concurrent;
using static APES.Program;

namespace APES
{
    public class MessagesHandler
    {
        private ConcurrentDictionary<ulong, MatchInstance> _matches;
        private Random _random;

        public MessagesHandler(ConcurrentDictionary<ulong, MatchInstance> matches)
        {
            _matches = matches;
            _random = new Random();
        }

        public async Task OnMessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot) return;

            SocketGuild? guild = (message.Channel as SocketGuildChannel)?.Guild;
            if (guild == null) return;

            Data.GuildSettings? guildSettings = DatabaseServices.TryGetCachedGuildSettings(guild);

            string cmdChar = guildSettings != null ? guildSettings.CommandChar : Config.commandTriggers.commandChar;
            string[] startList = guildSettings != null ? guildSettings.StartMatchKeywords : Config.commandTriggers.startMatch;
            string[] helpList = guildSettings != null ? guildSettings.HelpKeywords : Config.commandTriggers.help;
            string[] responses = Config.defaultResponses;

            if (CheckMessageForKeywords(startList, message.Content, cmdChar))
            {
                await MatchServices.AddMatch(message, _matches);
            }
            else if (CheckMessageForKeywords(startList, message.Content))
            {
                await ReplyAsync(message, $"Are you talking to me?\nTry again with {cmdChar} at the start");
            }
            else if (CheckMessageForKeywords(helpList, message.Content))
            {
                await message.Channel.SendMessageAsync(embed: EmbedFactory.BuildHelpEmbed(guildSettings, Config.helpText), components: ButtonFactory.BuildHelpButtons());
            }
            else if (Config.useReactions && CheckMessageForKeywords(Config.defaultReactionTriggerWords, message.Content))
                // this part is disabled because there is no way for the Admins to edit the guild settings yet
                //((guildSettings == null && CheckMessageForKeywords(Config.defaultReactionTriggerWords, message.Content)))
                // ||  (guildSettings != null && CheckForBotReactions(guildSettings.BotReactions, message.Content, out responses)))
            {
                var response = responses[_random.Next(responses.Length)];
                await ReplyAsync(message, response);
            }
            else if (message.Content.StartsWith($"{cmdChar}swap"))
            {
                await MatchServices.HandlePlayerSwap(message, _matches);
            }
            else if(message.Content.StartsWith($"{cmdChar}{Config.commandTriggers.settingsKeyword}"))
            {
                // TODO: implement guild settings UI & Logic 
            }
            else if (message.Content.StartsWith($"{cmdChar}ranks"))
            {
                Dictionary<string, (int, int, int)> leaderBoardData = await RankServices.GetLeaderBoardAsync(guild.Id);
                await message.Channel.SendMessageAsync(embed: EmbedFactory.BuildLeaderBoardEmbed(leaderBoardData), components: ButtonFactory.BuildCloseButton());
            }
        }

        public async Task OnModalSubmitted(SocketModal modal)
        {
            MatchInstance match = _matches.FirstOrDefault(m => m.Value.matchId == modal.Data.CustomId).Value;

            if (match != null)
            {
                var players = modal.Data.Components.First(x => x.CustomId == "names_input").Value;
                var split = players.Split(':');
                if (split.Length != 2)
                {
                    return;
                }

                int index1 = int.Parse(split[0].Trim()) - 1;
                int index2 = int.Parse(split[1].Trim()) - 1;

                await MatchServices.HandleSwapModalMessage(index1, index2, match);
            }

            await modal.DeferAsync();
        }

        private bool CheckMessageForKeywords(string[] keywordsArray, string message, string prefix = "")
        {
            return keywordsArray.Any(keyword => message.StartsWith(prefix + keyword, StringComparison.OrdinalIgnoreCase));
        }

        private bool CheckForBotReactions(List<Data.Reaction> reactions, string message, out string[] responses)
        {
            foreach (var reaction in reactions)
            {
                if(CheckMessageForKeywords(reaction.TriggerWords, message))
                {
                    responses = reaction.Responses;
                    return true;
                }
            }

            responses = new string[0];
            return false;
        }

        private async Task ReplyAsync(SocketMessage message, string response)
        {
            await message.Channel.SendMessageAsync(response);
        }
    }
}

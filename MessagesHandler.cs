// APES is free and open-source software licensed under AGPL-3.0. See LICENSE file for details.
using Discord.Net;
using Discord.WebSocket;
using NodaTime.Text;
using NodaTime;
using System.Collections.Concurrent;
using System.ComponentModel;
using static APES.Program;
using System.Globalization;
using Discord;

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

            if (guildSettings == null) return;

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
            else if (guildSettings.UseReactions && CheckMessageForKeywords(Config.defaultReactionTriggerWords, message.Content))
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
            // first try to get match ID as this is the most common use case
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
            else
            {
                var modalIdSplit = modal.Data.CustomId.Split(':');
                if(modalIdSplit.Length > 2 && (modalIdSplit[1] == SessionRequestActions.Date || modalIdSplit[1] == SessionRequestActions.Add))
                {
                    string guid = modalIdSplit[2];
                    string dateText = modalIdSplit[3];
                    string messageIdText = modalIdSplit.Length == 5 ? modalIdSplit[4] : null;

                    if (requestsInSetup.TryGetValue(guid, out var request))
                    {
                        var slot = request.TimeSlots.FirstOrDefault(ts => ts.Date == dateText);
                        if (modalIdSplit[1] == SessionRequestActions.Add)
                            slot = null;

                        string startTime = modal.Data.Components.First(x => x.CustomId == SessionRequestActions.StartTime).Value;
                        string duration = modal.Data.Components.First(x => x.CustomId == SessionRequestActions.Duration).Value;
                        string[] formats = { "h\\:mm", "hh\\:mm", "H\\:mm", "HH\\:mm" };
                        if (TimeSpan.TryParseExact(startTime, formats, null, out TimeSpan parsedStart) && TimeSpan.TryParseExact(duration, formats, null, out TimeSpan parsedDuration))
                        {
                            if (slot == null)
                            {
                                slot = new Data.TimeSlot();
                                slot.Date = dateText;
                                request.TimeSlots.Add(slot);
                            }
                            slot.Start = GetUTCTime(slot.Date, parsedStart.ToString(@"hh\:mm"), request.TimeZone);
                            slot.Duration = parsedDuration.ToString(@"hh\:mm");

                            if(modalIdSplit[1] == SessionRequestActions.Add)
                            {
                                if(requestUserEphemerals.TryGetValue(request.Guid, out var inter))
                                {
                                    await inter.ModifyOriginalResponseAsync(m => { m.Embed = EmbedFactory.BuildSessionRequestEmbed(request); m.Components = ButtonFactory.BuildSessionRequestButtons(request); });
                                    await modal.DeferAsync();
                                    await modal.DeleteOriginalResponseAsync();
                                }
                            }
                            else
                            {
                                await modal.UpdateAsync(m => { m.Embed = EmbedFactory.BuildSessionRequestEmbed(request); m.Components = ButtonFactory.BuildSessionRequestButtons(request); }) ;
                            }
                        }
                        else
                        {
                            await modal.RespondAsync($"❌ Invalid time format: `{startTime}`. Please enter the time in HH:mm format (e.g., 14:30).", ephemeral: true);
                            return;
                        }
                    }
                }
            }

            await modal.DeferAsync();
        }

        private string GetUTCTime(string date, string time, string timeZoneId)
        {
            // Parse the stored date and time
            var localDate = LocalDatePattern.Iso.Parse(date).Value;
            var localTime = LocalTimePattern.CreateWithInvariantCulture("HH:mm").Parse(time).Value;

            // Combine into a LocalDateTime
            var localDateTime = localDate + localTime;

            // Get the Noda Time zone
            var zone = DateTimeZoneProviders.Tzdb[timeZoneId];

            // Convert to UTC Instant
            var utcInstant = localDateTime.InZoneLeniently(zone).ToInstant();
            var utcTime = utcInstant.InUtc().TimeOfDay;

            return LocalTimePattern.CreateWithInvariantCulture("HH:mm").Format(utcTime); 
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

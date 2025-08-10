// APES is free and open-source software licensed under AGPL-3.0. See LICENSE file for details.
using Discord.WebSocket;
using Discord;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace APES
{
    public class MatchServices
    {
        public static async Task AddMatch(SocketMessage message, ConcurrentDictionary<ulong, MatchInstance> matchDict)
        {
            (int[] matchConfig, bool limitMatch, bool preTeams) = ParseMatchCommand(message.Content);
            MatchInstance match = CreateMatch(matchConfig,limitMatch, preTeams, message.Author, (message.Channel as SocketGuildChannel)!);
            await PostMatchMessageAsync(message.Channel, match, matchDict);
        }

        public static async Task AddMatchFromSlash(SocketTextChannel channel, SocketUser owner, int[] matchConfig, bool limitMatch, bool preTeams, ConcurrentDictionary<ulong, MatchInstance> matchDict)
        {
            MatchInstance match = CreateMatch(matchConfig, limitMatch, preTeams, owner, channel);
            await PostMatchMessageAsync(channel, match, matchDict);
        }

        /// <summary>
        /// Parse the Start Match Command
        /// </summary>
        /// <param name="command"></param>
        /// <returns>return an array with the match config, if its a single item array its a # of teams, if its a 2 item array its a specific match setup</returns>
        private static (int[], bool, bool) ParseMatchCommand(string command)
        {
            var lowerCaseCommand = command.ToLower();

            var teamMatch = Regex.Match(lowerCaseCommand, @"(\d+)\s*teams?");
            var vsMatch = Regex.Match(lowerCaseCommand, @"(\d+)v(\d+)");
            bool limitMatch = !lowerCaseCommand.Contains("--nl");
            bool preTeams = lowerCaseCommand.Contains("--pre");

            if (teamMatch.Success)
            {
                int.TryParse(teamMatch.Groups[1].Value, out int teams);
                return ([teams], false, preTeams);
            }
            else if (vsMatch.Success)
            {
                int left = int.TryParse(vsMatch.Groups[1].Value, out var l) ? l : 0;
                int right = int.TryParse(vsMatch.Groups[2].Value, out var r) ? r : 0;
                return ([left, right], limitMatch, false);
            }
            else
            {
                return ([2], false, preTeams);
            }
        }

        private static MatchInstance CreateMatch(int[] matchConfig, bool limitMatch, bool preTeams, SocketUser owner, IGuildChannel guildChannel)
        {
            MatchInstance match = new MatchInstance();
            match.matchId = Guid.NewGuid().ToString();
            match.owner = owner;
            match.state = MatchState.open;
            match.PlayerList = new List<ulong>();
            match.Teams = new List<List<ulong>>();
            match.TimeoutAt = DateTime.UtcNow.AddMinutes(5);
            match.MatchType = matchConfig;
            match.limitPlayerCount = limitMatch;
            match.preTeams = preTeams;
            match.split = false;
            match.Channel = guildChannel;

            return match;
        }

        private static async Task PostMatchMessageAsync(IMessageChannel channel, MatchInstance match, ConcurrentDictionary<ulong, MatchInstance> matchDict)
        {
            Embed embed = await EmbedFactory.BuildMatchEmbed(match);
            MessageComponent buttons = ButtonFactory.BuildMatchButtons(match);

            var msg = await channel.SendMessageAsync(embed: embed, components: buttons);
            match.Message = msg;
            if(matchDict.TryAdd(msg.Id, match))
            {
                await match.StartTimerAsync(async () =>
                {
                    await RemoveMatchAsync(msg, matchDict);
                });
            }
        }

        public static async Task SplitMatch(MatchInstance match, SocketMessage message, ConcurrentDictionary<ulong, MatchInstance> matchDict)
        {
            if (match.Teams.Count <= 2) return;

            List<ulong> leftOut = new List<ulong>();
            for (int i = 0; i < match.Teams.Count; i++)
                leftOut.AddRange(match.Teams[i]);

            List<ulong> used = new List<ulong>();
            IGuildChannel guildChannel = (message.Channel as SocketGuildChannel)!;

            for (int i = 0; i < match.Teams.Count; i++)
            {
                if(i % 2 == 1)
                {
                    MatchInstance newMatch = CreateMatch(match.MatchType, true, false, match.owner, guildChannel);

                    List<ulong> team1 = match.Teams[i - 1];
                    List<ulong> team2 = match.Teams[i];

                    newMatch.PlayerList.AddRange(team1);
                    newMatch.PlayerList.AddRange(team2);

                    used.AddRange(team1);
                    used.AddRange(team2);

                    newMatch.Teams.Add(team1);
                    newMatch.Teams.Add(team2);

                    newMatch.TimeoutAt = DateTime.UtcNow.AddMinutes(60);
                    newMatch.state = MatchState.rolled;
                    newMatch.split = true;

                    await PostMatchMessageAsync(message.Channel, newMatch, matchDict);
                }
            }

            leftOut = leftOut.Except(used).ToList();
            if(leftOut.Count > 0)
            {
                MatchInstance newMatch = CreateMatch([2], true, false, match.owner, guildChannel);
                newMatch.PlayerList = leftOut;
                newMatch.TimeoutAt = DateTime.UtcNow.AddMinutes(60);
                newMatch.split = true;
                await PostMatchMessageAsync(message.Channel, newMatch, matchDict);
            }

            if (matchDict.TryRemove(message.Id, out var expiredMatch))
            {
                expiredMatch.CancelTimer();
                await message.DeleteAsync();
            }
        }

        public static async Task RemoveMatchAsync(IUserMessage message, ConcurrentDictionary<ulong, MatchInstance> matchDict)
        {
            if (matchDict.TryRemove(message.Id, out var expiredMatch))
            {
                expiredMatch.CancelTimer();
                await EditMessageToClosedMatch(message);
            }
        }

        private static async Task EditMessageToClosedMatch(IUserMessage message)
        {
            
            await message.ModifyAsync(m =>
            {
                m.Components = new ComponentBuilder().Build();
            });
        }

        /// <summary>
        /// Divides players into teams based on MatchType, either try and divide the players into equal sized teams,
        /// or divide them according to the match type team sizes ie; 2v2, 1v2 etc..
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public static List<List<ulong>> DividePlayersIntoTeams(MatchInstance match)
        {
            List<List<ulong>> teams = new List<List<ulong>>();

            if(match.preTeams == false)
            {
                // Shuffle players randomly
                var shuffled = match.PlayerList.OrderBy(_ => Guid.NewGuid()).ToList();

                if (match.MatchType.Length == 2 && match.MatchType.Sum() > 0) // specific match type - 1v1 2v2 3v3 etc....
                {
                    // Try to divide by matchType, e.g. 1v3, 2v2, etc.
                    int totalRequired = match.MatchType.Sum();
                    int playersCount = shuffled.Count;
                    int pairsNeeded = playersCount / totalRequired;  // full pairs we can make

                    if (playersCount < totalRequired)
                    {
                        // Not enough players: fill teams proportionally but with available players
                        double scale = (double)playersCount / totalRequired;

                        int team1Count = (int)Math.Round(match.MatchType[0] * scale);
                        int team2Count = playersCount - team1Count;

                        teams.Add(shuffled.Take(team1Count).ToList());
                        teams.Add(shuffled.Skip(team1Count).Take(team2Count).ToList());
                    }
                    else
                    {
                        // Enough or more players than needed for at least one full pair
                        int currentIndex = 0;

                        for (int i = 0; i < pairsNeeded; i++)
                        {
                            // Take matchType[0] players for team1 in this pair
                            teams.Add(shuffled.GetRange(currentIndex, match.MatchType[0]));
                            currentIndex += match.MatchType[0];

                            // Take matchType[1] players for team2 in this pair
                            teams.Add(shuffled.GetRange(currentIndex, match.MatchType[1]));
                            currentIndex += match.MatchType[1];
                        }

                        // Remaining players after full pairs go into overflow team(s)
                        int remaining = playersCount - currentIndex;
                        if (remaining > 0)
                        {
                            teams.Add(shuffled.GetRange(currentIndex, remaining));
                        }
                    }
                }
                else if (match.MatchType.Length == 1) // teams count match
                {
                    // Equal division of players into teamsCount teams
                    int teamsCount = match.MatchType[0];
                    int playersPerTeam = shuffled.Count / teamsCount;
                    int remainder = shuffled.Count % teamsCount;
                    int currentIndex = 0;

                    for (int i = 0; i < teamsCount; i++)
                    {
                        // Distribute remainder players one by one to first teams
                        teams.Add(shuffled.GetRange(currentIndex, playersPerTeam));
                        currentIndex += playersPerTeam;
                    }

                    if (remainder > 0)
                    {
                        Queue<ulong> leftOut = new Queue<ulong>(shuffled.GetRange(currentIndex, remainder));
                        int k = 0;
                        while (leftOut.Count > 0)
                        {
                            teams[k % teamsCount].Add(leftOut.Dequeue());
                            k++;
                        }
                    }
                }
            }
            else if (match.MatchType.Length == 1 && match.preTeams == true) // predefined teams match
            {
                var rnd = new Random();
                teams = match.Teams.OrderBy(t => rnd.Next()).ToList();
            }

            match.state = MatchState.rolled;

            return teams;
        }

        public static void SwapPlayers(MatchInstance match, SocketGuildUser player1, SocketGuildUser player2)
        {
            if (match.Teams.Count < 2 || player1 == null || player2 == null)
                return;

            int team1Index = -1, team2Index = -1;
            int player1Index = -1, player2Index = -1;

            // Find players in teams
            for (int i = 0; i < match.Teams.Count; i++)
            {
                var team = match.Teams[i];

                if (team.Contains(player1.Id))
                {
                    team1Index = i;
                    player1Index = team.IndexOf(player1.Id);
                }

                if (team.Contains(player2.Id))
                {
                    team2Index = i;
                    player2Index = team.IndexOf(player2.Id);
                }
            }

            // Make sure both players were found
            if (team1Index == -1 || team2Index == -1)
                return;

            // Swap them
            match.Teams[team1Index][player1Index] = player2.Id;
            match.Teams[team2Index][player2Index] = player1.Id;
        }
    
        public static bool VarifyMatchPerm(MatchInstance match, SocketGuildUser user)
        {
            if(user.Id == match.owner.Id || user.GuildPermissions.Administrator || user.GuildPermissions.ModerateMembers)
                return true;
            else
                return false;
        }

        public static async Task HandlePlayerSwap(SocketMessage message, ConcurrentDictionary<ulong, MatchInstance> matchDict)
        {
            var (user1, user2) = ParseSwapUsers(message);

            if (user1 == null || user2 == null)
            {
                await message.Channel.SendMessageAsync("You need to mention 2 players to swap", messageReference: message.Reference);
                return;
            }

            if (message.Reference == null || !matchDict.TryGetValue(message.Reference.MessageId.Value, out var match))
            {
                await message.Channel.SendMessageAsync("You need to reply to a Match message", messageReference: message.Reference);
            }
            else if (match != null && user1 != null && user2 != null)
            {
                var guild = (message.Channel as SocketGuildChannel)?.Guild;
                if (guild != null)
                {
                    SocketGuildUser guildUser = guild.GetUser(message.Author.Id);

                    if (VarifyMatchPerm(match, guildUser))
                    {
                        SwapPlayers(match, guild.GetUser((ulong)user1), guild.GetUser((ulong)user2));
                        await match.Message.ModifyAsync(async m => m.Embed = await EmbedFactory.BuildMatchEmbed(match));
                    }
                }
            }
        }

        private static (ulong?, ulong?) ParseSwapUsers(SocketMessage message)
        {
            ulong?[] users = new ulong?[2];

            int u = 0;
            for (int i = 0; i < message.MentionedUsers.Count; i++)
            {
                SocketUser? usr = message.MentionedUsers.ElementAtOrDefault(i);
                if (usr != null && !usr.IsBot)
                {
                    users[u] = usr.Id;
                    u++;
                }
            }

            return (users[0], users[1]);
        }

        public static async Task HandleSwapModalMessage(int index1, int index2, MatchInstance match)
        {
            if(match.Teams.Count == 2)
            {
                var team1 = match.Teams[0];
                var team2 = match.Teams[1];

                if(index1 < team1.Count && index1 >= 0 && index2 < team2.Count && index2 >= 0)
                {
                    ulong player1 = team1[index1];
                    ulong player2 = team2[index2];

                    team1.Insert(index1, player2);
                    team2.Insert(index2, player1);

                    team1.Remove(player1);
                    team2.Remove(player2);

                    await match.Message.ModifyAsync(async m => m.Embed = await EmbedFactory.BuildMatchEmbed(match));
                }
            }
        }
    
        public static bool CheckVacancy(MatchInstance match)
        {
            if(match.limitPlayerCount == false) 
                return true;

            int maxPlayers = int.MaxValue;

            if (match.MatchType.Length == 2)
                maxPlayers = match.MatchType.Sum();

            if (match.PlayerList.Count < maxPlayers)
                return true;
            else
                return false;
        }

        public static async Task<ulong> AddFakeUserForDebug(MatchInstance match, SocketMessageComponent component)
        {
            var guild = (component.Channel as SocketGuildChannel)?.Guild;
            if (guild == null)
                return default;

            var fakeUsers = await DatabaseServices.GetFakeUsers();

            var availableFakeUsers = fakeUsers
                .Where(u => !match.PlayerList.Contains(u.UserId))
                .ToList();

            ulong fakeId;

            if (availableFakeUsers.Count > 0)
            {
                fakeId = availableFakeUsers[new Random().Next(availableFakeUsers.Count)].UserId;
            }
            else
            {
                fakeId = Program.FakeIdThreshold + (ulong)new Random().Next(1, 1000);
                await DatabaseServices.GetOrCreateDiscordUserData(guild, fakeId);
            }

            match.PlayerList.Add(fakeId);

            return fakeId;
        }
    }

    public enum MatchState
    {
        open,
        rolled,
        locked,
        waitingForResults,
        done
    }

    public class MatchInstance
    {
        public string matchId { get; set; }
        public SocketUser owner { get; set; }
        public MatchState state { get; set; }
        public List<ulong> PlayerList { get; set; }
        public List<List<ulong>> Teams { get; set; }
        public int[] MatchType { get; set; } // {1, 1} = 1v1 | {1, 3} = 1v3    - Or -    {2} | {3} | {6} for num of teams
        public bool limitPlayerCount { get; set; }
        public bool preTeams { get; set; } // predefined teams
        public bool split { get; set; }
        public IUserMessage Message { get; set; }
        public IGuildChannel Channel { get; set; }
        public DateTime TimeoutAt { get; set; }
        public Task Timer { get; set; }

        private CancellationTokenSource _cts = new();
        private Task? _timerTask;

        public void SetTimeout(TimeSpan fromNow)
        {
            TimeoutAt = DateTime.UtcNow.Add(fromNow);
        }

        public void ExtendTimeout(TimeSpan extra)
        {
            TimeoutAt = TimeoutAt.Add(extra);
        }

        public async Task StartTimerAsync(Func<Task> onTimeout)
        {
            _cts = new CancellationTokenSource();
            _timerTask = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    var now = DateTime.UtcNow;
                    if (now >= TimeoutAt)
                    {
                        await onTimeout();
                        break;
                    }

                    await Task.Delay(1000, _cts.Token); // check every 1 second
                }
            });
        }

        public void CancelTimer()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
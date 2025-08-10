// APES is free and open-source software licensed under AGPL-3.0. See LICENSE file for details.
using APES.Data;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using static APES.Program;


namespace APES
{
    public class DatabaseServices
    {
        public static ApesDbContext DB;

        public static Dictionary<ulong, GuildSettings> guildSettings;

        public DatabaseServices(ApesDbContext db)
        {
            DB = db;
            guildSettings = new Dictionary<ulong, GuildSettings>();
        }

        public static async Task EnsureGuildsDataAsync(DiscordSocketClient client)
        {
            foreach (var guild in client.Guilds)
            {
                if (!TryGetGuildData(guild.Id, out _))
                {
                    await CreateGuildDataAsync(guild);
                }

                guildSettings.Add(guild.Id, CloneGuildSettings(guild));
            }
        }

        private static GuildSettings CloneGuildSettings(SocketGuild? guild)
        {
            if (guild == null) return null;

            GuildSettings? dbSettings = DB.Guilds
                .Include(g => g.GuildSettings).ThenInclude(s => s.BotReactions)
                .Include(g => g.GuildSettings).ThenInclude(s => s.RolePermissions)
                .FirstOrDefault(g => g.GuildId == guild.Id).GuildSettings;

            GuildSettings cache = new GuildSettings()
            {
                CommandChar = dbSettings.CommandChar,
                HelpKeywords = dbSettings.HelpKeywords,
                StartMatchKeywords = dbSettings.StartMatchKeywords,
                UseReactions = dbSettings.UseReactions
            };

            cache.RolePermissions = new List<RolePermission>();

            foreach (var perm in dbSettings.RolePermissions)
            {
                cache.RolePermissions.Add(new RolePermission()
                {
                    RoleId = perm.RoleId,
                    CanModifyMatch = perm.CanModifyMatch,
                    CanModifySettings = perm.CanModifySettings,
                });
            }

            cache.BotReactions = new List<Reaction>();

            foreach (var reaction in dbSettings.BotReactions)
            {
                cache.BotReactions.Add(new Reaction()
                {
                    TriggerWords = reaction.TriggerWords,
                    Responses = reaction.Responses,
                });
            }

            return cache;
        }

        public static bool TryGetGuildData(ulong guildId, out GuildData? guildData)
        {
            guildData = DB.Guilds.FirstOrDefault(g => g.GuildId == guildId);

            if (guildData == null)
                return false;
            else
                return true;
        }

        public static async Task CreateGuildDataAsync(SocketGuild guild)
        {
            GuildData newGuildData = new GuildData()
            {
                GuildId = guild.Id,
                Name = guild.Name,
                MaxTournaments = 1,
                Tournamens = new List<TournamentData>() 
                { 
                    new TournamentData 
                    {
                        Participants = new List<Participant>() 
                    } 
                },
                GuildSettings = new GuildSettings()
                {
                    CommandChar = Config.commandTriggers.commandChar,
                    StartMatchKeywords = Config.commandTriggers.startMatch,
                    HelpKeywords = Config.commandTriggers.help,
                    UseReactions = Config.useReactions,
                    RolePermissions = new List<RolePermission>(),
                    BotReactions = new List<Reaction>()
                    {
                        new Reaction()
                        {
                            TriggerWords = Config.defaultReactionTriggerWords,
                            Responses = Config.defaultResponses,
                        }
                    }
                },
            };

            DB.Guilds.Add(newGuildData);
            await DB.SaveChangesAsync();
        }

        public static async Task<GuildData?> GetGuildData(ulong guildId)
        {
            return await DB.Guilds.Where(g => g.GuildId == guildId).FirstOrDefaultAsync();
        }

        public static async Task<GuildSettings?> GetGuildSettingsAsync(SocketGuild guild)
        {
            return await DB.Guilds
                .Include(g => g.GuildSettings).ThenInclude(s => s.BotReactions)
                .Include(g => g.GuildSettings).ThenInclude(s => s.RolePermissions)
                .Where(g => g.GuildId == guild.Id)
                .Select(g => g.GuildSettings)
                .FirstOrDefaultAsync();
        }

        public static GuildSettings? TryGetCachedGuildSettings(SocketGuild guild)
        {
            return TryGetCachedGuildSettings(guild.Id);
        }

        public static GuildSettings? TryGetCachedGuildSettings(ulong guildId)
        {
            guildSettings.TryGetValue(guildId, out GuildSettings? settings);
            return settings;
        }

        public static async Task<TournamentData?> GetFullTournamentDataAsync(ulong guildId, int tournamentIndex = 0)
        {
            var guild = await DB.Guilds
                                .Include(g => g.Tournamens!)
                                    .ThenInclude(t => t.Participants)
                                .FirstOrDefaultAsync(g => g.GuildId == guildId);

            if (guild == null || guild.Tournamens == null || tournamentIndex < 0 || tournamentIndex >= guild.Tournamens.Count)
                return null;

            return guild.Tournamens[tournamentIndex];
        }

        public static async Task<List<Participant>?> GetTournamentParticipantsDataAsync(ulong guildId, int tournamentIndex = 0)
        {
            var guild = await DB.Guilds
                                .Include(g => g.Tournamens!)
                                    .ThenInclude(t => t.Participants)
                                .FirstOrDefaultAsync(g => g.GuildId == guildId);

            if (guild == null || guild.Tournamens == null || tournamentIndex < 0 || tournamentIndex >= guild.Tournamens.Count)
                return null;

            return guild.Tournamens[tournamentIndex].Participants;
        }

        /// <summary>
        /// Get Discord User Data for Opt in/out
        /// </summary>
        /// <param name="user">SocketUser</param>
        /// <returns></returns>
        public static async Task<DiscordUserData?> GetOrCreateDiscordUserData(SocketUser user)
        {
            var userData = await DB.DiscordUserDatas.FirstOrDefaultAsync(u => u.UserId == user.Id);
        
            if (userData == null)
            {
                userData = new DiscordUserData
                {
                    UserId = user.Id,
                    UserName = user.Username,
                    OptInTournamentId = new List<int>()
                };
        
                DB.DiscordUserDatas.Add(userData);
                await DB.SaveChangesAsync();
            }
        
            return userData;
        }

        /// <summary>
        /// get Data for matches display, this include fake debug users
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static async Task<DiscordUserData?> GetOrCreateDiscordUserData(SocketGuild guild, ulong userId)
        {
            var userData = await DB.DiscordUserDatas.FirstOrDefaultAsync(u => u.UserId == userId);

            if (userData == null)
            {
                string userName = string.Empty;
                if(Config.debug && userId > FakeIdThreshold) // fake user
                {
                    userName = $"Fake User {DB.DiscordUserDatas.Count()}";
                }
                else // guild user
                {
                    userName = guild.GetUser(userId).Username;
                }

                userData = new DiscordUserData
                {
                    UserId = userId,
                    UserName = userName,
                    OptInTournamentId = new List<int>()
                };

                DB.DiscordUserDatas.Add(userData);
                await DB.SaveChangesAsync();
            }

            return userData;
        }

        public static async Task<List<DiscordUserData>> GetFakeUsers()
        {
            if (DB == null || DB.DiscordUserDatas == null || DB.DiscordUserDatas.Count() == 0)
                return new List<DiscordUserData>();

            var allUsers = await DB.DiscordUserDatas.ToListAsync();
            var fakeUsers = allUsers.Where(u => u.UserId >= FakeIdThreshold).ToList();
            return fakeUsers;
        }

        public static DiscordUserData? TryGetDiscordUserData(Participant participant)
        {
            return DB.DiscordUserDatas.FirstOrDefault(u => u.Id == participant.DiscordUserDataId);
        }

        public static DiscordUserData? TryGetDiscordUserData(SocketUser socketUser)
        {
            return DB.DiscordUserDatas.FirstOrDefault(u => u.UserId == socketUser.Id);
        }

        public static async Task AddParticipantToTournamentData(TournamentData tournamentData, Participant participant)
        {
            tournamentData.Participants!.Add(participant);
            await DB.SaveChangesAsync();
        }

        public static async Task SaveDB()
        {
            await DB.SaveChangesAsync();
        }

        public static async Task OptInData(SocketUser user)
        {
            var userData = await GetOrCreateDiscordUserData(user);

            if(userData != null)
            {
                userData.hideScore = false;
                userData.optOutData = false;
            }

            await DB.SaveChangesAsync();
        }

        public static async Task HidePlayerScore(SocketUser user)
        {
            var userData = await GetOrCreateDiscordUserData(user);

            if(userData != null)
                userData.hideScore = true;

            await DB.SaveChangesAsync();
        }

        public static async Task<bool> RemoveAllPlayerData(SocketUser user, bool savePreferences)
        {
            bool succeeded = false;

            await RemoveAllTournamentDataForUser(user);

            var userData = TryGetDiscordUserData(user);

            if (userData != null)
            {
                if (savePreferences)
                {
                    userData.UserName = "_";
                    userData.hideScore = true;
                    userData.optOutData = true;
                }
                else
                {
                    DB.DiscordUserDatas.Remove(userData);
                }

                succeeded = true;
            }
            
            if(succeeded)
                await DB.SaveChangesAsync();

            return succeeded;
        }

        public static async Task RemoveAllTournamentDataForUser(SocketUser user)
        {
            var userData = TryGetDiscordUserData(user);
            if (userData == null) return;

            var guilds = await DB.Guilds
                .Include(g => g.Tournamens!)
                    .ThenInclude(t => t.Participants!)
                .ToListAsync();

            bool changed = false;

            foreach (var guild in guilds)
            {
                foreach (var tournament in guild.Tournamens!)
                {
                    var toRemove = tournament.Participants!
                        .Where(p => p.DiscordUserDataId == userData.Id)
                        .ToList();

                    if (toRemove.Count > 0)
                    {
                        foreach (var participant in toRemove)
                            tournament.Participants!.Remove(participant);

                        changed = true;
                    }
                }
            }

            if (changed)
                await DB.SaveChangesAsync();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace APES.Data
{
    public class ApesDbContext : DbContext
    {
        public DbSet<GuildData> Guilds { get; set; }
        public DbSet<DiscordUserData> DiscordUserDatas { get; set; }
        public DbSet<GuildSettings> GuildSettings{ get; set; } // New DBset
        public DbSet<TournamentData> TournamentDatas { get; set; } // New DBset
        public DbSet<TournamentMatch> TournamentMatches { get; set; } // New DBset
        public DbSet<TournamentChallange> TournamentChallanges { get; set; } // New DBset

        public ApesDbContext(DbContextOptions<ApesDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
               base.OnModelCreating(modelBuilder);
        }
    }
}

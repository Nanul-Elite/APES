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

        public ApesDbContext(DbContextOptions<ApesDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
               base.OnModelCreating(modelBuilder);
        }
    }
}

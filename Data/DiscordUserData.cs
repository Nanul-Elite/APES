using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APES.Data
{
    public class DiscordUserData
    {
        public int Id { get; set; }
        public ulong UserId { get; set; }
        public string UserName { get; set; }
        public bool hideScore { get; set; }
        public bool optOutData { get; set; }
        public List<int>? OptInTournamentId { get; set; }
    }
}

using System.Collections.Generic;
using System.Linq;

namespace DiscordGivewayBot.Data.Models.Entities
{
    public class DiscordGiveawayBot
    {
        public long Id { get; set; }
        public List<DiscordGuild> Guilds { get; set; }
        public string Emoji { get; set; }

        public DiscordGiveawayBot(long id, IEnumerable<DiscordGuild> guilds, string emoji)
        {
            Id = id;
            Guilds = guilds.ToList();
            Emoji = emoji;
        }
    }
}

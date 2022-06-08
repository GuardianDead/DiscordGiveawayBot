namespace DiscordGivewayBot.Data.Models.Entities
{
    public class DiscordGiveawayBot
    {
        public long Id { get; set; }
        public DiscordGuild Guild { get; set; }
        public string Emoji { get; set; }

        public DiscordGiveawayBot(long id, DiscordGuild guild, string emoji)
        {
            Id = id;
            Guild = guild;
            Emoji = emoji;
        }
    }
}

namespace GivewayCheck.Models
{
    public class DiscordGiveawayBot
    {
        public string Id { get; set; }
        public string Emoji { get; set; }

        public DiscordGiveawayBot(string id, string emoji)
        {
            Id = id;
            Emoji = emoji;
        }
    }
}

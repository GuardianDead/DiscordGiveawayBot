namespace GivewayCheck.Models
{
    public class DiscordServer
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DiscordGiveawayBot GiveawayBot { get; set; }

        public DiscordServer(string id, string name, DiscordGiveawayBot giveawayBot)
        {
            Id = id;
            Name = name;
            GiveawayBot = giveawayBot;
        }
    }
}

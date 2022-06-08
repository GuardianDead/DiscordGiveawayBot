namespace DiscordGivewayBot.Data.Models.Entities
{
    public class DiscordGuild
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public DiscordGuild(long id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}

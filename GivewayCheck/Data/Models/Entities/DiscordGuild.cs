namespace DiscordGivewayBot.Data.Models.Entities
{
    public class DiscordGuild
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long WLRoleId { get; set; }

        public DiscordGuild(long id, string name, long wlRoleId)
        {
            Id = id;
            Name = name;
            WLRoleId = wlRoleId;
        }
    }
}

namespace DiscordGivewayBot.Data.Models.Entities
{
    public class DiscordAccount
    {
        public long Id { get; set; }
        public string Token { get; set; }
        public Proxy Proxy { get; set; }

        public DiscordAccount(long id, string token, Proxy proxy)
        {
            Id = id;
            Token = token;
            Proxy = proxy;
        }
    }
}

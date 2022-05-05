using GivewayCheck.Domain;

namespace GivewayCheck.Models
{
    public class DiscordAccount
    {
        public string Token { get; set; }
        public Proxy Proxy { get; set; }

        public DiscordAccount(string token, Proxy proxy)
        {
            Token = token;
            Proxy = proxy;
        }
    }
}

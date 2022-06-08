using DiscordGivewayBot.Data.Models.Entities;
using GivewayCheck.Domain;
using GivewayCheck.Services;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace GivewayCheck.Controllers
{
    public class DiscordAccountController
    {
        public DiscordAccountController()
        {
        }

        public async Task<IEnumerable<DiscordAccount>> ReadAccountsAsync(string discordAccountsPath)
        {
            await LogService.LogMessageAsync("Reading discord accounts from a file...");
            var discordAccountStrings = await File.ReadAllLinesAsync(discordAccountsPath);

            var discordAccounts = new List<DiscordAccount>();
            foreach (var discordAccountString in discordAccountStrings)
            {
                var discordAccountStringValues = discordAccountString.Split(';', ':');
                Proxy discordAccountProxy = null;

                if (discordAccountStringValues.Length != 1)
                    discordAccountProxy = new Proxy(discordAccountStringValues[1], int.Parse(discordAccountStringValues[2]), discordAccountStringValues[3], discordAccountStringValues[4]);

                var discordAccountId = await ReadAccountIdAsync(discordAccountStringValues[0], discordAccountProxy);

                discordAccounts.Add(new DiscordAccount(discordAccountId, discordAccountStringValues[0], discordAccountProxy));
            }

            return discordAccounts;
        }

        private async Task<long> ReadAccountIdAsync(string token, Proxy? proxy)
        {
            var client = CreateRestClient(proxy);
            var request = new RestRequest("https://discord.com/api/v9/users/@me");
            request.AddHeader("authorization", token);

            var response = await client.GetAsync(request);
            if (!response.IsSuccessful)
                await LogService.LogMessageAsync($"Failed to get token id when reading discord accounts - {token}");

            return long.Parse(JObject.Parse(response.Content)["id"].ToString());
        }
        private RestClient CreateRestClient(Proxy? proxy) => proxy is null ?
        new RestClient() :
        new RestClient(new RestClientOptions()
        {
            Proxy = new WebProxy(proxy.Address, proxy.Port)
            {
                Credentials = new NetworkCredential(proxy.Login, proxy.Password)
            }
        });
    }
}

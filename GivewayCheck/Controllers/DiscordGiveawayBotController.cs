using DiscordGivewayBot.Data.Models;
using DiscordGivewayBot.Data.Models.Entities;
using GivewayCheck.Services;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace GivewayCheck.Controllers
{
    public class DiscordGiveawayBotController
    {
        public DiscordGiveawayBotController()
        {
        }

        public async ValueTask<IEnumerable<DiscordGiveawayBot>> ReadDiscordGiveawayBotsAsync(string discordGiveawayBotsPath, DiscordAccount[] discordAccounts)
        {
            await LogService.LogMessageAsync("Reading discord giveaway bots from a file...");

            var discordGiveawayBots = new List<DiscordGiveawayBot>();
            var discordGiveawayBotsJson = JObject.Parse(await File.ReadAllTextAsync(discordGiveawayBotsPath));
            var discordAccountsIndex = 0;
            var discordRequest = new RestRequest();
            foreach (var discordGiveawayBotJson in JArray.Parse(discordGiveawayBotsJson["discordGiveawayBots"].ToString()))
            {
                var discordGiveawayBotGuilds = new List<DiscordGuild>();
                foreach (var discordGiveawayBotGuildJson in JArray.Parse(discordGiveawayBotJson["discordServers"].ToString()))
                {
                    var discordClient = CreateClient(discordAccounts[discordAccountsIndex]?.Proxy);
                    discordRequest.Resource = $"https://discord.com/api/v9/guilds/{discordGiveawayBotGuildJson["id"]}";
                    discordRequest.AddOrUpdateHeader("authorization", discordAccounts[discordAccountsIndex].Token);
                    var discordGuildResponse = await discordClient.ExecuteGetAsync(discordRequest);
                    if (!discordGuildResponse.IsSuccessful)
                    {
                        if (discordGuildResponse.Content.Contains("TooManyRequests") && discordGuildResponse.Content.Contains("rate limited"))
                            await LogService.LogMessageAsync($"Discord account got a timeout - {discordAccounts[discordAccountsIndex].Token}");
                        else if (discordGuildResponse.Content.Contains("MissingAccess"))
                            await LogService.LogMessageAsync($"Discord account missing access - {discordAccounts[discordAccountsIndex].Token}");
                        else
                            await LogService.LogMessageAsync($"Discord account got a timeout - {discordAccounts[discordAccountsIndex].Token}");
                        discordAccountsIndex++;
                        if (discordAccountsIndex == discordAccounts.Length)
                            discordAccountsIndex = 0;
                        continue;
                    }

                    discordGiveawayBotGuilds.Add(new DiscordGuild(long.Parse(discordGiveawayBotGuildJson["id"].ToString()), JObject.Parse(discordGuildResponse.Content)["name"].ToString(), long.Parse(discordGiveawayBotGuildJson["WLRoleId"].ToString())));
                }
                discordGiveawayBots.Add(new DiscordGiveawayBot(long.Parse(discordGiveawayBotJson["id"].ToString()), discordGiveawayBotGuilds, discordGiveawayBotJson["emoji"].ToString()));
            }

            return discordGiveawayBots;
        }
        private RestClient CreateClient(Proxy proxy) => proxy is null ?
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

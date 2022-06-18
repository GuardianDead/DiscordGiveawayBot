using DiscordGivewayBot.Data.Models;
using DiscordGivewayBot.Data.Models.Entities;
using GivewayCheck.Services;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DiscordGivewayBot.Controllers
{
    public class DiscordRoleController
    {
        public DiscordRoleController()
        {
        }

        public async ValueTask<Dictionary<DiscordGuild, List<DiscordAccount>>> GetDiscordAccountsWithGuildRolesAsync(DiscordAccount[] discordAccounts, DiscordGuild[] discordGuilds)
        {
            var discordAccountsWithGuildRoles = new Dictionary<DiscordGuild, List<DiscordAccount>>();
            foreach (var discordGuild in discordGuilds)
                discordAccountsWithGuildRoles.Add(discordGuild, await GetDiscordAccountsWithGuildRoleAsync(discordAccounts, discordGuild));
            return discordAccountsWithGuildRoles;
        }
        public async ValueTask<List<DiscordAccount>> GetDiscordAccountsWithGuildRoleAsync(DiscordAccount[] discordAccounts, DiscordGuild discordGuild)
        {
            var discordAccountsWithGuildRole = new List<DiscordAccount>();
            await Parallel.ForEachAsync(discordAccounts, async (discordAccount, cancellationToken) =>
            {
                if (await CheckDiscordAccountForGuildRoleAsync(discordAccount, discordGuild))
                    discordAccountsWithGuildRole.Add(discordAccount);
            });
            return discordAccountsWithGuildRole;
        }
        public async Task<bool> CheckDiscordAccountForGuildRoleAsync(DiscordAccount discordAccount, DiscordGuild discordGuild)
        {
            var discordClient = CreateClient(discordAccount?.Proxy);
            var discordRequest = new RestRequest($"https://discord.com/api/v9/users/@me/guilds/{discordGuild.Id}/member");
            discordRequest.AddHeader("authorization", discordAccount.Token);

            while (true)
            {
                var discordAccountGuildRolesResponse = await discordClient.ExecuteGetAsync(discordRequest);
                if (!discordAccountGuildRolesResponse.IsSuccessful)
                {
                    if (discordAccountGuildRolesResponse.Content.Contains("You are being rate limited"))
                    {
                        await LogService.LogMessageAsync($"Discord account got a timeout - {discordAccount.Token}");
                        var delayInSeconds = int.Parse(Math.Ceiling(double.Parse(JObject.Parse(discordAccountGuildRolesResponse.Content)["retry_after"].ToString())).ToString());
                        await Task.Delay(delayInSeconds * 1000);
                    }
                    else if (discordAccountGuildRolesResponse.Content.Contains("MissingAccess"))
                        await LogService.LogMessageAsync($"Discord account missing access - {discordAccount.Token}");
                    else
                        await LogService.LogMessageAsync($"An unknown error occurred when checking the discord account for a role in the guild - {discordAccountGuildRolesResponse.Content}");
                    continue;
                }

                return JArray.Parse(JObject.Parse(discordAccountGuildRolesResponse.Content)["roles"].ToString()).Any(role => role.Value<string>() == discordGuild.WLRoleId.ToString());
            }
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

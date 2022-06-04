using DiscordGivewayBot.Configurations;
using GivewayCheck.Domain;
using GivewayCheck.Models;
using GivewayCheck.Services;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace GivewayCheck.Controllers
{
    public class DiscordGiveawayController
    {
        List<string> badPhrases = new List<string>();
        List<string> participateGiveaways = new List<string>();
        List<string> endedGiveaways = new List<string>();
        LaunchConfigurations configurations = new LaunchConfigurations();

        public DiscordGiveawayController()
        {
        }

        public async Task CheckGiveawaysAsync(DiscordAccount[] discordAccounts, DiscordGuild[] discordGuilds)
        {
            badPhrases = (await File.ReadAllLinesAsync(configurations.BadGiveawayPhrasesPath)).ToList();
            if (File.Exists(configurations.ParticipateGiveawaysPath))
                participateGiveaways = (await File.ReadAllLinesAsync(configurations.ParticipateGiveawaysPath)).ToList();
            if (File.Exists(configurations.EndedGiveawaysPath))
                endedGiveaways = (await File.ReadAllLinesAsync(configurations.EndedGiveawaysPath)).ToList();

            await LogService.LogMessageAsync("Сhecking discord guilds for giveaway...");
            var discordRequest = new RestRequest();

            var discordGuildsIndex = 0;
            var discordAccountsIndex = 0;
            while (discordGuildsIndex < discordGuilds.Count())
            {
                discordRequest.AddHeader("authorization", discordAccounts[discordAccountsIndex].Token);
                var restClient = CreateRestClient(discordAccounts[discordAccountsIndex]?.Proxy);
                discordRequest.Resource = $"https://discord.com/api/v9/guilds/{discordGuilds[discordGuildsIndex].Id}" +
                    $"/messages/search?author_id={discordGuilds[discordGuildsIndex].GiveawayBot.Id}";

                var lastBotMessagesRespounce = await restClient.ExecuteGetAsync(discordRequest);
                if (!lastBotMessagesRespounce.IsSuccessful)
                {
                    if (lastBotMessagesRespounce.Content.Contains("TooManyRequests") && lastBotMessagesRespounce.Content.Contains("rate limited"))
                        await LogService.LogMessageAsync($"Discord account got a timeout - {discordAccounts[discordAccountsIndex].Token}");
                    else if (lastBotMessagesRespounce.Content.Contains("MissingAccess"))
                        await LogService.LogMessageAsync($"Discord account missing access - {discordAccounts[discordAccountsIndex].Token}");
                    else
                        await LogService.LogMessageAsync($"Discord account got a timeout or missing access - {discordAccounts[discordAccountsIndex].Token}");
                    discordAccountsIndex++;
                    if (discordAccountsIndex == discordAccounts.Count())
                        discordAccountsIndex = 0;
                    continue;
                }

                var lastBotMessages = JArray.Parse(JObject.Parse(lastBotMessagesRespounce.Content)["messages"].ToString());
                foreach (var message in lastBotMessages)
                {
                    var messagePath = $@"{discordGuilds[discordGuildsIndex].Id}/{message.First["channel_id"]}/{message.First["id"]}";
                    if (CheckMessageForСontinuingGiveaway(message, messagePath))
                    {
                        await LogService.LogMessageAsync($"Giveaway found by suitable parameters - {messagePath}");
                        await ReactMessageFromAllDiscordAccountAsync(discordAccounts, discordRequest, discordGuilds[discordGuildsIndex], message);
                    }
                    if (CheckMessageForСontinuingRumbleButtle(message, messagePath))
                    {
                        await LogService.LogMessageAsync($"Rumble Battle found by suitable parameters - {messagePath}");
                        await ReactMessageFromAllDiscordAccountAsync(discordAccounts, discordRequest, discordGuilds[discordGuildsIndex], message);
                    }
                    if (CheckGiveawayForEnded(message, messagePath))
                    {
                        await LogService.LogMessageAsync($"This giveaway is ended - {messagePath}");
                        participateGiveaways.Remove(messagePath);
                        endedGiveaways.Add(messagePath);
                        await File.WriteAllLinesAsync(configurations.ParticipateGiveawaysPath, participateGiveaways);
                        await File.WriteAllLinesAsync(configurations.EndedGiveawaysPath, endedGiveaways);
                    }
                }

                discordGuildsIndex++;
            }
        }
        private bool CheckMessageForСontinuingGiveaway(JToken message, string messagePath)
        {
            var hasBadPrhase = !string.IsNullOrEmpty(message.First?["embeds"].First?["author"]?["name"].ToString()) && badPhrases.Any(badPrhase => message.First["embeds"].First["author"]["name"].ToString().ToLower().Contains(badPrhase.ToLower()));
            var isContainsInParticipateGiveaways = participateGiveaways.Contains(messagePath);
            var haveСontinuingGiveawayDescription = !string.IsNullOrEmpty(message.First?["embeds"].First?["footer"]?["text"].ToString()) &&
                !string.IsNullOrEmpty(message.First?["embeds"].First?["author"]?["name"].ToString()) &&
                (message.First["embeds"].First["footer"]["text"].ToString().Contains("Ends") ||
                message.First["embeds"].First["footer"]["text"].ToString().Contains("winner"));

            return !hasBadPrhase && !isContainsInParticipateGiveaways && haveСontinuingGiveawayDescription;
        }
        private bool CheckMessageForСontinuingRumbleButtle(JToken message, string messagePath)
        {
            var isContainsInParticipateGiveaways = participateGiveaways.Contains(messagePath);
            var haveRumbleBattleDescriptions = !string.IsNullOrEmpty(message.First?["embeds"].First?["description"]?.ToString()) &&
                message.First["embeds"].First["description"].ToString().Contains("Click the emoji below to join");

            return !isContainsInParticipateGiveaways && haveRumbleBattleDescriptions;
        }
        private bool CheckGiveawayForEnded(JToken message, string messagePath)
        {
            var isContainsInParticipateGiveaways = participateGiveaways.Contains(messagePath);
            var isContainsInEndedGiveaways = endedGiveaways.Contains(messagePath);
            var haveEndedGiveawayDescription = !string.IsNullOrEmpty(message.First?["embeds"].First?["footer"]?["text"].ToString()) &&
                message.First["embeds"].First["footer"]["text"].ToString().Contains("Ended");

            return isContainsInParticipateGiveaways && !isContainsInEndedGiveaways && haveEndedGiveawayDescription;
        }
        private async Task ReactMessageFromAllDiscordAccountAsync(IEnumerable<DiscordAccount> discordAccounts, RestRequest discordRequest, DiscordGuild discordServer, JToken message)
        {
            var channelId = message.First["channel_id"].ToString();
            var messageId = message.First["id"].ToString();
            var giveawayPath = $@"{discordServer.Id}/{channelId}/{messageId}";

            var addedReactDiscordAccountsCount = 0;
            discordRequest.Resource = $"https://discord.com/api/v9/channels/{channelId}/messages/{messageId}/reactions/{discordServer.GiveawayBot.Emoji}/@me";
            foreach (var discordAccount in discordAccounts)
            {
                var client = CreateRestClient(discordAccount?.Proxy);
                discordRequest.AddOrUpdateHeader("authorization", discordAccount.Token);
                var resultRespounce = await client.ExecutePutAsync(discordRequest);
                if (!resultRespounce.IsSuccessful)
                {
                    if (resultRespounce.Content.Contains("Unauthorized"))
                        await LogService.LogMessageAsync($"Failed to react {discordAccount.Token}|{giveawayPath} the discord account access token is expire");
                    else if (resultRespounce.Content.Contains("Access"))
                        await LogService.LogMessageAsync($"Failed to react {discordAccount.Token}|{giveawayPath} - this discord account does not have a discord guild or it does not have access to the channel where giveaway.");
                    else
                        await LogService.LogMessageAsync($"Failed to react - {discordAccount.Token}|{giveawayPath} - by an unknown error: {resultRespounce.Content}");
                }
                else
                    addedReactDiscordAccountsCount++;

                await Task.Delay(new Random().Next(configurations.MinDelayEachDiscordAccountReact, configurations.MaxDelayEachDiscordAccountReact));
            }

            participateGiveaways.Add(giveawayPath);
            await File.AppendAllTextAsync(configurations.ParticipateGiveawaysPath, giveawayPath + Environment.NewLine);
            await LogService.LogMessageAsync($"{addedReactDiscordAccountsCount}/{discordAccounts.Count()} put a reaction on message giveaway - {giveawayPath}");
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

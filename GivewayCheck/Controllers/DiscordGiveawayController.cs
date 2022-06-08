using DiscordGivewayBot.Configurations;
using DiscordGivewayBot.Data.Models;
using DiscordGivewayBot.Data.Models.Entities;
using GivewayCheck.Domain;
using GivewayCheck.Services;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GivewayCheck.Controllers
{
    public class DiscordGiveawayController
    {
        List<string> badPhrases = new List<string>();
        List<string> participateGiveaways = new List<string>();
        List<DiscordRumbleBattle> participateRumbleBattles = new List<DiscordRumbleBattle>();
        List<string> wonGiveaways = new List<string>();
        List<string> wonRumbleBattles = new List<string>();
        readonly LaunchConfigurations configurations = new LaunchConfigurations();

        public DiscordGiveawayController()
        {
        }

        public async Task CheckDiscordGiveawaysAndRumbleBattlesAsync(DiscordAccount[] discordAccounts, DiscordGiveawayBot[] discordGiveawayBots)
        {
            badPhrases = (await File.ReadAllLinesAsync(configurations.BadGiveawayPhrasesPath)).ToList();
            if (File.Exists(configurations.ParticipateGiveawaysPath))
                participateGiveaways = (await File.ReadAllLinesAsync(configurations.ParticipateGiveawaysPath)).ToList();
            if (File.Exists(configurations.ParticipateRumbleBattlesPath))
                participateRumbleBattles = (await File.ReadAllLinesAsync(configurations.ParticipateRumbleBattlesPath)).Select(participateRumbleBattleStirng =>
                {
                    var splittedParticipateRumbleBattleStirng = participateRumbleBattleStirng.Split(';');
                    return new DiscordRumbleBattle(splittedParticipateRumbleBattleStirng[0], DateTime.Parse(splittedParticipateRumbleBattleStirng[1]));
                }).ToList();
            if (File.Exists(configurations.WonGiveawaysPath))
                wonGiveaways = (await File.ReadAllLinesAsync(configurations.WonGiveawaysPath)).Select(wonGiveawayString => wonGiveawayString.Split(';').First()).ToList();
            if (File.Exists(configurations.WonRumbleBattlesPath))
                wonRumbleBattles = (await File.ReadAllLinesAsync(configurations.WonRumbleBattlesPath)).Select(wonGiveawayString => wonGiveawayString.Split(';').First()).ToList();

            await LogService.LogMessageAsync("Сhecking discord giveaway bots for giveaway...");
            var discordRequest = new RestRequest();
            var discordGiveawayBotsIndex = 0;
            var discordAccountsIndex = 0;
            while (discordGiveawayBotsIndex < discordGiveawayBots.Length)
            {
                discordRequest.AddHeader("authorization", discordAccounts[discordAccountsIndex].Token);
                var restClient = CreateRestClient(discordAccounts[discordAccountsIndex]?.Proxy);
                discordRequest.Resource = $"https://discord.com/api/v9/guilds/{discordGiveawayBots[discordGiveawayBotsIndex].Guild.Id}" +
                    $"/messages/search?author_id={discordGiveawayBots[discordGiveawayBotsIndex].Id}";

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
                    if (discordAccountsIndex == discordAccounts.Length)
                        discordAccountsIndex = 0;
                    continue;
                }

                var lastDiscordGiveawayBotMessages = JArray.Parse(JObject.Parse(lastBotMessagesRespounce.Content)["messages"].ToString());
                foreach (var message in lastDiscordGiveawayBotMessages)
                {
                    var messagePath = $@"{discordGiveawayBots[discordGiveawayBotsIndex].Id}/{message.First["channel_id"]}/{message.First["id"]}";
                    if (CheckMessageForСontinuingGiveaway(message, messagePath))
                    {
                        await LogService.LogMessageAsync($"Giveaway found by suitable parameters - {messagePath}");
                        await ReactMessageFromAllDiscordAccountAsync(discordAccounts, discordRequest, discordGiveawayBots[discordGiveawayBotsIndex], message);
                        participateGiveaways.Add(messagePath);
                        await File.AppendAllTextAsync(configurations.ParticipateGiveawaysPath, messagePath + Environment.NewLine);
                    }
                    else if (CheckMessageForEndedGiveaway(message, messagePath))
                    {
                        await LogService.LogMessageAsync($"This giveaway is ended - {messagePath}");
                        foreach (var discordAccount in discordAccounts)
                            if (CheckWinningDiscordAccount(discordAccount.Id, message.First["embeds"].First["description"].ToString()))
                            {
                                await LogService.LogMessageAsync($"Discord account won in giveaway - {discordAccount.Token};{messagePath};{discordGiveawayBots[discordGiveawayBotsIndex].Guild.Name}");
                                await File.AppendAllTextAsync(configurations.WonGiveawaysPath, $"{messagePath};{discordAccount.Token};{discordGiveawayBots[discordGiveawayBotsIndex].Guild.Name}" + Environment.NewLine);
                                if (!wonGiveaways.Contains(messagePath))
                                    wonGiveaways.Add(messagePath);
                            }
                        participateGiveaways.Remove(messagePath);
                        await File.WriteAllLinesAsync(configurations.ParticipateGiveawaysPath, participateGiveaways);
                    }
                    else if (CheckMessageForСontinuingRumbleBattle(message, messagePath))
                    {
                        await LogService.LogMessageAsync($"Rumble battle found by suitable parameters - {messagePath}");
                        await ReactMessageFromAllDiscordAccountAsync(discordAccounts, discordRequest, discordGiveawayBots[discordGiveawayBotsIndex], message);
                        participateRumbleBattles.Add(new DiscordRumbleBattle(messagePath, DateTime.Parse(message.First["timestamp"].ToString())));
                        await File.AppendAllTextAsync(configurations.ParticipateRumbleBattlesPath, $"{messagePath};{message.First["timestamp"]}" + Environment.NewLine);
                    }
                    else if (CheckMessageForEndedRumbleBattle(message, messagePath))
                    {
                        await LogService.LogMessageAsync($"This rumble battle is ended - {messagePath}");
                        foreach (var discordAccount in discordAccounts)
                            if (CheckWinningDiscordAccount(discordAccount.Id, message.First["mentions"].First["id"].ToString()))
                            {
                                await LogService.LogMessageAsync($"Discord account won in rumble battle - {discordAccount.Token};{messagePath};{discordGiveawayBots[discordGiveawayBotsIndex].Guild.Name}");
                                await File.AppendAllTextAsync(configurations.WonRumbleBattlesPath, $"{messagePath};{discordAccount.Token};{discordGiveawayBots[discordGiveawayBotsIndex].Guild.Name}" + Environment.NewLine);
                                if (!wonRumbleBattles.Contains(messagePath))
                                    wonRumbleBattles.Add(messagePath);
                            }
                    }
                }

                discordGiveawayBotsIndex++;
            }
        }

        #region Classic Giveaway
        public bool CheckMessageForСontinuingGiveaway(JToken message, string messagePath)
        {
            var haveGiveawayDescription = !string.IsNullOrEmpty(message.First?["embeds"].First?["footer"]?["text"].ToString()) &&
                !string.IsNullOrEmpty(message.First?["embeds"].First?["author"]?["name"].ToString()) &&
                (message.First["embeds"].First["footer"]["text"].ToString().Contains("Ends") ||
                message.First["embeds"].First["footer"]["text"].ToString().Contains("winner"));
            var isContainsInParticipateGiveaways = participateGiveaways.Contains(messagePath);
            var isContainsInWonGiveaways = wonGiveaways.Contains(messagePath);
            var hasBadPrhase = !string.IsNullOrEmpty(message.First?["embeds"].First?["author"]?["name"].ToString()) && badPhrases.Any(badPrhase => message.First["embeds"].First["author"]["name"].ToString().ToLower().Contains(badPrhase.ToLower()));

            return haveGiveawayDescription && !isContainsInParticipateGiveaways && !isContainsInWonGiveaways && !hasBadPrhase;
        }
        public bool CheckMessageForEndedGiveaway(JToken message, string messagePath)
        {
            var haveEndedGiveawayDescription = !string.IsNullOrEmpty(message.First?["embeds"].First?["footer"]?["text"].ToString()) &&
                message.First["embeds"].First["footer"]["text"].ToString().Contains("Ended");
            var isContainsInParticipateGiveaways = participateGiveaways.Contains(messagePath);
            var isContainsInWonGiveaways = wonGiveaways.Contains(messagePath);

            return haveEndedGiveawayDescription && isContainsInParticipateGiveaways && !isContainsInWonGiveaways;
        }
        #endregion
        #region Rumble Battle
        public bool CheckMessageForСontinuingRumbleBattle(JToken message, string messagePath)
        {
            var isExprised = DateTime.Parse(message.First["timestamp"].ToString()).AddMinutes(5) < DateTime.Now;
            var haveСontinuingRumbleBattleDescriptions = !string.IsNullOrEmpty(message.First?["embeds"].First?["description"]?.ToString()) &&
                message.First["embeds"].First["description"].ToString().Contains("Click the emoji below to join");
            var isContainsInParticipateRumbleBattles = participateRumbleBattles.Any(participateRumbleBattl => participateRumbleBattl.Path == messagePath);
            var isContainsInWonRumbleBattles = wonRumbleBattles.Contains(messagePath);

            return haveСontinuingRumbleBattleDescriptions && !isExprised && !isContainsInParticipateRumbleBattles && !isContainsInWonRumbleBattles;
        }
        public bool CheckMessageForEndedRumbleBattle(JToken message, string messagePath)
        {
            var haveEndedRumbleBattleDescriptions = !string.IsNullOrEmpty(message.First?["embeds"].First?["title"]?.ToString()) &&
                message.First["embeds"].First["title"].ToString().Contains("WINNER");
            var isContainsInParticipateRumbleBattles = participateRumbleBattles.Any(participateRumbleBattle => participateRumbleBattle.Path.StartsWith(messagePath[..37]) && participateRumbleBattle.DateTime < DateTime.Parse(message.First["timestamp"].ToString()));
            var isContainsInWonRumbleBattles = wonRumbleBattles.Any(wonRumbleBattle => wonRumbleBattle == messagePath);

            return haveEndedRumbleBattleDescriptions && isContainsInParticipateRumbleBattles && !isContainsInWonRumbleBattles;
        }
        #endregion

        public bool CheckWinningDiscordAccount(long discordAccountId, string giveawayMessage)
        {
            var giveawayWinnerIds = new Regex("\\d{18}").Matches(giveawayMessage);
            return giveawayWinnerIds.Any(giveawayWinnerId => long.Parse(giveawayWinnerId.Value) == discordAccountId);
        }
        public async Task ReactMessageFromAllDiscordAccountAsync(IEnumerable<DiscordAccount> discordAccounts, RestRequest discordRequest, DiscordGiveawayBot discordGiveawayBot, JToken message)
        {
            var channelId = message.First["channel_id"].ToString();
            var messageId = message.First["id"].ToString();
            var giveawayPath = $@"{discordGiveawayBot.Guild.Id}/{channelId}/{messageId}";
            discordRequest.Resource = $"https://discord.com/api/v9/channels/{channelId}/messages/{messageId}/reactions/{discordGiveawayBot.Emoji}/@me";

            var addedReactDiscordAccountsCount = 0;
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

﻿using GivewayCheck.Domain;
using GivewayCheck.Models;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace GivewayCheck
{
    class Program
    {
        static private string currentPath = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.FullName;
        static private string participateGiveawayPath = $@"{currentPath}/Actual giveaways.txt";
        static private string participateEndedGiveawayPath = $@"{currentPath}/Ended giveaways.txt";

        static private List<string> participateGiveawayList = new List<string>();
        static private List<string> participateEendedGiveawayList = new List<string>();
        static private List<DiscordAccount> discordAccounts;
        static private List<DiscordServer> discordServers;

        static async Task Main(string[] args)
        {
            discordServers = await ReadAllDiscordGiveawayServersAsync();
            discordAccounts = ReadAllAwalableDiscordAccounts(2, 4, 21, 9);
            if (File.Exists(participateGiveawayPath))
                participateGiveawayList = (await File.ReadAllLinesAsync(participateGiveawayPath)).ToList();
            if (File.Exists(participateGiveawayPath))
                participateEendedGiveawayList = (await File.ReadAllLinesAsync(participateEndedGiveawayPath)).ToList();
            var discordRequest = new RestRequest();
            //discordRequest = new RestRequest(@$"https://discord.com/api/v9/channels/895006097444864000/messages");
            discordRequest.AddHeader("tts", false);
            //discordRequest.AddHeader("authorization", discordAccounts.First().Token);
            //var result = await new RestClient().ExecuteGetAsync(discordRequest);
            while (true)
            {
                Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Checking discord servers for giveaways...");
                var discordServerIndex = 0;
                var discordAccountIndex = 0;
                while (discordServerIndex < discordServers.Count)
                {
                    discordRequest.AddHeader("authorization", discordAccounts[discordAccountIndex].Token);
                    RestClient restClient = CreateRestClient(discordAccounts[discordAccountIndex]?.Proxy);
                    discordRequest.Resource = $"https://discord.com/api/v9/guilds/{discordServers[discordServerIndex].Id}" +
                        $"/messages/search?author_id={discordServers[discordServerIndex].GiveawayBot.Id}";
                    var lastBotMessagesRespounce = await restClient.ExecuteGetAsync(discordRequest);
                    if (!lastBotMessagesRespounce.IsSuccessful)
                    {
                        discordAccountIndex++;
                        if (discordAccountIndex == discordAccounts.Count)
                            discordAccountIndex = 0;
                        continue;
                    }
                    var lastBotMessages = JArray.Parse(JObject.Parse(lastBotMessagesRespounce.Content)["messages"].ToString());
                    foreach (var message in lastBotMessages)
                    {
                        var giveawayPath = $@"{discordServers[discordServerIndex].Id}/{message.First["channel_id"]}/{message.First["id"]}";
                        await CheckMessageForGiveaway(message, giveawayPath, discordRequest, discordServerIndex);
                        await CheckGiveawayForEnded(giveawayPath, message);
                    }
                    discordServerIndex++;
                }
            }
        }

        static private async Task CheckMessageForGiveaway(JToken message, string giveawayPath, RestRequest discordRequest, int discordServerIndex)
        {
            if (DateTime.Now < DateTime.Parse(message.First["timestamp"].ToString()).AddDays(2) &&
                !participateGiveawayList.Contains(giveawayPath) &&
                !string.IsNullOrEmpty(message.First?["embeds"].First?["footer"]?["text"].ToString()) &&
                message.First["embeds"].First["footer"]["text"].ToString().Contains("Ends"))
                await ReactMessageFromAllDiscordAccountAsync(discordRequest, discordServers[discordServerIndex], message);
        }
        static private async Task CheckGiveawayForEnded(string giveawayPath, JToken message)
        {
            if (participateGiveawayList.Contains(giveawayPath) &&
                !participateEendedGiveawayList.Contains(giveawayPath) &&
                !string.IsNullOrEmpty(message.First?["embeds"].First?["footer"]?["text"].ToString()) &&
                message.First["embeds"].First["footer"]["text"].ToString().Contains("Ended"))
            {
                Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] this {giveawayPath} giveaway is ended");
                participateGiveawayList.Remove(giveawayPath);
                participateEendedGiveawayList.Add(giveawayPath);
                await File.WriteAllLinesAsync(participateGiveawayPath, participateGiveawayList);
                await File.WriteAllLinesAsync(participateEndedGiveawayPath, participateEendedGiveawayList);
            }
        }
        static private async Task ReactMessageFromAllDiscordAccountAsync(RestRequest discordRequest, DiscordServer discordServer, JToken message)
        {
            var channelId = message.First["channel_id"].ToString();
            var messageId = message.First["id"].ToString();
            var giveawayPath = $@"{discordServer.Id}/{channelId}/{messageId}";

            var addedReactDiscordAccountsCount = 0;
            discordRequest.Resource = $"https://discord.com/api/v9/channels/{channelId}/messages/{messageId}/reactions/{discordServer.GiveawayBot.Emoji}/@me";
            foreach (var discordAccount in discordAccounts)
            {
                RestClient client = CreateRestClient(discordAccount?.Proxy);
                discordRequest.AddOrUpdateHeader("authorization", discordAccount.Token);
                var resultRespounce = await client.ExecutePutAsync(discordRequest);
                if (!resultRespounce.IsSuccessful)
                {
                    Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Failed to react {discordAccount.Proxy.Address} " +
                        $"to the message {giveawayPath}: {resultRespounce.Content}");
                }
                else
                    addedReactDiscordAccountsCount++;
            }
            participateGiveawayList.Add($@"{discordServer.Id}/{channelId}/{messageId}");
            await File.WriteAllLinesAsync(participateGiveawayPath, participateGiveawayList);
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] {addedReactDiscordAccountsCount}/{discordAccounts.Count} " +
                    $"червя бахнули лайки - {discordServer.Id}/{channelId}/{messageId}");
            Console.Beep(2000, 1000);
        }
        static private RestClient CreateRestClient(Proxy proxy)
        {
            if (proxy is not null)
            {
                var clientOptions = new RestClientOptions()
                {
                    Timeout = 0,
                    Proxy = new WebProxy(proxy.Address, int.Parse(proxy.Port))
                    {
                        Credentials = new NetworkCredential(proxy.Login, proxy.Password)
                    }
                };
                return new RestClient(clientOptions);
            }
            else
                return new RestClient();
        }
        static private async Task<List<DiscordServer>> ReadAllDiscordGiveawayServersAsync()
        {
            var stringJsonDiscordSevers = JObject.Parse(await File.ReadAllTextAsync(@$"{currentPath}\Servers.json"));
            var jsonArrayDiscordServers = JArray.Parse(stringJsonDiscordSevers["discordServers"].ToString());
            return jsonArrayDiscordServers
                .Select(discordServer => new DiscordServer(
                    id: (string)discordServer["id"],
                    name: (string)discordServer["name"],
                    giveawayBot: new DiscordGiveawayBot(
                        id: (string)discordServer["giveawayBot"]["id"],
                        emoji: (string)discordServer["giveawayBot"]["emoji"])))
                .ToList();
        }
        static private List<DiscordAccount> ReadAllAwalableDiscordAccounts(int fromRow, int fromColumn, int toRow, int toColumn)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var discordAccounts = new List<DiscordAccount>();
            var sheet = new ExcelPackage(@"C:\Users\kakaw\OneDrive\Рабочий стол\Аккаунты.xlsx").Workbook.Worksheets.First();
            var resultValues = (object[,])sheet.Cells[fromRow, fromColumn, toRow, toColumn].Value;
            for (int rowIndex = 0; rowIndex <= 19; rowIndex++)
            {
                if (string.IsNullOrEmpty(resultValues[rowIndex, 0]?.ToString()) || resultValues[rowIndex, 5].ToString() == "0")
                    continue;
                if (!string.IsNullOrEmpty(resultValues[rowIndex, 1]?.ToString()))
                {
                    var proxy = new Proxy(resultValues[rowIndex, 1].ToString(), resultValues[rowIndex, 2].ToString(),
                        resultValues[rowIndex, 3].ToString(), resultValues[rowIndex, 4].ToString());
                    discordAccounts.Add(new DiscordAccount(resultValues[rowIndex, 0].ToString(), proxy));
                }
                else
                    discordAccounts.Add(new DiscordAccount(resultValues[rowIndex, 0].ToString(), null));
            }
            discordAccounts.Reverse();
            return discordAccounts;
        }
    }
}
